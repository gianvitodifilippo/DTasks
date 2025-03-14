CREATE DATABASE approvals_db;

\c approvals_db;

CREATE TABLE approvers (
    id UUID PRIMARY KEY,
    name TEXT NOT NULL,
    email TEXT UNIQUE NOT NULL
);

INSERT INTO approvers (id, name, email) VALUES
('550e8400-e29b-41d4-a716-446655440000', 'John Doe', 'john.doe@example.com'),
('d1b20e52-5d4b-4fdc-b4b1-3f11b7eaffc2', 'Jane Smith', 'jane.smith@example.com');