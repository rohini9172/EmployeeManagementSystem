import { createContext, useContext, useState, useEffect } from 'react';
import api from '../services/api';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null);

  // loading = true while we verify the stored token on app startup.
  // This prevents a flash of the login page before the token check completes.
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const token = localStorage.getItem('token');
    if (!token) {
      setLoading(false);
      return;
    }

    // Call GET /api/auth/me to verify the stored token is still valid.
    // If the token expired or was tampered with, the server returns 401
    // and the Axios interceptor in api.js clears localStorage + redirects to /login.
    api.get('/auth/me')
      .then((res) => {
        // Token is valid — restore user state from the server response
        setUser({
          token,
          userId: res.data.userId,
          name: res.data.name,
          email: res.data.email,
          role: res.data.role,
        });
      })
      .catch(() => {
        // Token invalid or expired — clear everything
        localStorage.clear();
        setUser(null);
      })
      .finally(() => setLoading(false));
  }, []);

  const login = (authResponse) => {
    // NOTE: localStorage is used here for simplicity in development.
    // In production, prefer HttpOnly cookies to prevent XSS token theft.
    // With HttpOnly cookies the JS cannot read the token at all —
    // the browser sends it automatically on every request.
    localStorage.setItem('token', authResponse.token);
    setUser(authResponse);
  };

  const logout = () => {
    localStorage.clear();
    setUser(null);
  };

  // Don't render children until we know if the user is logged in.
  // This prevents protected routes from briefly flashing before redirect.
  if (loading) {
    return <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh' }}>Loading...</div>;
  }

  return (
    <AuthContext.Provider value={{ user, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export const useAuth = () => useContext(AuthContext);
