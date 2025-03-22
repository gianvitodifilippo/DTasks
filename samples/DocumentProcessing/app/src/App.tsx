import { useEffect, useState } from "react";
import { ToastContainer, toast } from 'react-toastify';
import { v7 as uuid } from 'uuid';
import "./App.css";

const PORT = 5262;
const API_BASE = `http://localhost:${PORT}`;
const WS_URL = `ws://localhost:${PORT}/ws`;

type UploadResponse = {
  documentId: string;
  uploadUrl: string;
};

const App: React.FC = () => {
  const [documentId, setDocumentId] = useState<string | null>(null);
  const [uploadUrl, setUploadUrl] = useState<string | null>(null);
  const [fileName, setFileName] = useState<string | null>(null);
  const [operationId, setOperationId] = useState<string | null>(null);
  const [webSocket, setWebSocket] = useState<WebSocket | null>(null);
  const [connectionId, setConnectionId] = useState<string | null>(null);

  useEffect(() => {
    const socket = new WebSocket(WS_URL);
    const connectionId = uuid();

    socket.onopen = () => {
      socket.send(`connect:${connectionId}`);
      toast(`WebSockets connection established. Connection id: ${connectionId}`);
    };

    setWebSocket(socket);
    setConnectionId(connectionId);
  }, []);

  useEffect(() => {
    if (!webSocket)
      return;

    webSocket.onmessage = (event) => {
      const { operationId: messageOperationId } = JSON.parse(event.data) as { operationId: string };
      if (messageOperationId !== operationId)
        return;
      
      toast(`Document ${documentId} successfully processed!`);
    };
  }, [webSocket, operationId, documentId]);

  const requestUploadUrl = async () => {
    setDocumentId(null);
    setUploadUrl(null);
    setFileName(null);
    setOperationId(null);

    const response = await fetch(`${API_BASE}/upload-request`, {
      method: "POST",
    });

    if (!response.ok) {
      alert("Could not get upload URL.");
      return;
    }

    const payload: UploadResponse = await response.json();
    setDocumentId(payload.documentId);
    setUploadUrl(payload.uploadUrl);
  }

  const onFileSelected = async (e: React.ChangeEvent<HTMLInputElement>) => {
    if (!e.target.files || !uploadUrl)
      return;

    setFileName(null);
    setOperationId(null);

    const file = e.target.files[0];
    const response = await fetch(uploadUrl, {
      method: "PUT",
      headers: {
        "Content-Type": file.type,
        "x-ms-blob-type": "BlockBlob"
      },
      body: file,
    });

    if (!response.ok) {
      alert('Could not upload the file.');
      return;
    }

    setFileName(file.name);
  };

  const startProcessing = async () => {
    if (!documentId || !connectionId)
      return;

    setOperationId(null);

    const response = await fetch(`${API_BASE}/process-document/${documentId}`, {
      method: "POST",
      headers: {
        'Async-CallbackType': 'websockets',
        'Async-ConnectionId': connectionId
      }
    });

    if (!response.ok) {
      if (response.status === 404) {
        alert('Invalid document id.');
      }
      else {
        alert('Could not start processing.');
      }
      return;
    }
    
    const { operationId } = await response.json() as { operationId: string };
    setOperationId(operationId);
  }

  return (
    <div className="container">
      <div className="header">
        <h1>Document processing</h1>
        <h2>An example of integration of DTasks with the browser</h2>
      </div>
      <div className="upload">
        <div className="step1">
          <h3>Step 1 - Request an upload URL</h3>
          <p className="description">This will call the server to get a SAS URL to upload your file to the blob storage.</p>
          <button
            className="file-selection"
            onClick={requestUploadUrl}
          >
            Request URL
          </button>
          <p>{documentId ? `Document ID: ${documentId}` : ""}</p>
        </div>
        <div className="step2">
          <h3>Step 2 - Upload the file to the blob storage</h3>
          <p className="description">Select a file to upload using the SAS URL you receive from the server.</p>
          <label htmlFor="file-selection" className={`file-selection ${!uploadUrl ? "disabled" : ""}`}>
            Select file
          </label>
          <input
            id="file-selection"
            type="file"
            accept="application/pdf"
            onChange={onFileSelected}
            disabled={!uploadUrl}
          />
          {fileName ? (
            <p>Successfully uploaded <strong>{fileName}</strong></p>
          ) : (
            <p></p>
          )}
        </div>
        <div className="step3">
          <h3>Step 3 - Start the processing</h3>
          <p className="description">Inform the server that the document has been uploaded. The server will start processing.</p>
          <button
            className={`file-selection ${!fileName ? "disabled" : ""}`}
            onClick={startProcessing}
            disabled={!fileName}
          >
            Start processing
          </button>
          <p>{operationId ? "Processing started!": ""}</p>
        </div>
        <div className="step4">
          <h3>Step 4 - Receive notifications</h3>
          <p className="description">After processing is complete, the server will notify about the outcome.</p>
        </div>
      </div>
      <ToastContainer
        position="bottom-right"
        theme="dark"
        hideProgressBar
      />
    </div>
  );
};

export default App;
