import { useState, useCallback } from 'react';

// Wraps any async API call with loading and error state.
// Eliminates repetitive try/catch/finally in every component.
//
// Usage:
//   const { loading, error, execute } = useApi();
//   const data = await execute(() => getEmployees());
export default function useApi() {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const execute = useCallback(async (apiCall) => {
    setLoading(true);
    setError('');
    try {
      const result = await apiCall();
      return result;
    } catch (err) {
      const msg = err.response?.data?.message || err.message || 'Something went wrong.';
      setError(msg);
      return null;
    } finally {
      setLoading(false);
    }
  }, []);

  return { loading, error, execute, setError };
}
