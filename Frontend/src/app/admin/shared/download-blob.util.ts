// Triggers a browser download for an already-fetched Blob. Used instead of
// a plain <a href> for CSV export, since the export request needs the admin
// session cookie attached (withCredentials), which a simple navigation
// can't guarantee - the blob is fetched via HttpClient first instead.
export function downloadBlob(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  link.click();
  URL.revokeObjectURL(url);
}
