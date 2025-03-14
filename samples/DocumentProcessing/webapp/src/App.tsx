import React, { useState } from "react";
import { UploadResponse } from "./types";

const API_BASE = "http://localhost:5262";

const App: React.FC = () => {
  const [documentId, setDocumentId] = useState<string | null>(null);
  const [uploadUrl, setUploadUrl] = useState<string | null>(null);
  const [file, setFile] = useState<File | null>(null);
  const [processing, setProcessing] = useState<boolean>(false);
  const [status, setStatus] = useState<string>("");

  const requestUploadUrl = async () => {
    setStatus("Requesting upload URL...");
    const response = await fetch(`${API_BASE}/upload-request`, {
      method: "POST",
    });

    if (!response.ok) {
      setStatus("Failed to get upload URL.");
      return;
    }

    const data: UploadResponse = await response.json();
    setDocumentId(data.documentId);
    setUploadUrl(data.uploadUrl);
    setStatus("Upload URL received. Ready to upload.");
  };

  const uploadDocument = async () => {
    if (!file || !uploadUrl) {
      alert("Please select a file first.");
      return;
    }

    setStatus("Uploading document...");
    const response = await fetch(uploadUrl, {
      method: "PUT",
      headers: { "Content-Type": file.type },
      body: file,
    });

    if (response.ok) {
      setStatus("Upload complete. Ready to process.");
    } else {
      setStatus("Upload failed.");
    }
  };

  const processDocument = async () => {
    if (!documentId) return;

    setProcessing(true);
    setStatus("Processing started...");

    await fetch(`${API_BASE}/process-document/${documentId}`, {
      method: "POST",
    });

    const socket = new WebSocket("ws://localhost:5262/ws");

    socket.onopen = () => {
      console.log("WebSocket connected");
      socket.send(`subscribe:${documentId}`);
    };

    socket.onmessage = (event) => {
      setStatus(event.data);
      setProcessing(false);
      socket.close();
    };
  };

  return (
    <div className="flex flex-col items-center p-8 space-y-6 bg-gray-100 min-h-screen">
      <h1 className="text-2xl font-bold">Document Processing</h1>
      <button
        onClick={requestUploadUrl}
        className="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600"
      >
        Request Upload URL
      </button>

      {uploadUrl && (
        <>
          <input
            type="file"
            accept="application/pdf"
            onChange={(e) => setFile(e.target.files[0])}
            className="border p-2"
          />
          <button
            onClick={uploadDocument}
            className="bg-green-500 text-white px-4 py-2 rounded hover:bg-green-600"
          >
            Upload Document
          </button>
        </>
      )}

      {documentId && !processing && (
        <button
          onClick={processDocument}
          className="bg-yellow-500 text-white px-4 py-2 rounded hover:bg-yellow-600"
        >
          Process Document
        </button>
      )}

      <p className="text-lg font-semibold">{status}</p>
    </div>
  );
};

export default App;
