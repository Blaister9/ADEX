import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { App } from './App';

const apiBaseUrl = (import.meta.env.VITE_ADEX_API_BASE_URL as string | undefined) ?? '';

const container = document.getElementById('root');
if (!container) {
  throw new Error('Dashboard mount point #root is missing from index.html.');
}

createRoot(container).render(
  <StrictMode>
    <App apiBaseUrl={apiBaseUrl || 'http://localhost:5080'} />
  </StrictMode>,
);
