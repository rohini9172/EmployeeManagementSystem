import { useEffect } from 'react';

// Shows a temporary notification at the bottom-right of the screen.
// Automatically disappears after `duration` ms.
// Usage: <Toast message="Saved!" type="success" onClose={() => setToast(null)} />
export default function Toast({ message, type = 'success', onClose, duration = 3000 }) {
  useEffect(() => {
    const timer = setTimeout(onClose, duration);
    return () => clearTimeout(timer); // Cleanup if component unmounts early
  }, [onClose, duration]);

  return (
    <div className={`toast toast-${type}`} onClick={onClose} style={{ cursor: 'pointer' }}>
      {message}
    </div>
  );
}
