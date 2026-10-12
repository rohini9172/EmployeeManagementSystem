import axios from 'axios';

// Base URL for all API calls — matches the ASP.NET Core dev server
const api = axios.create({
  baseURL: 'http://localhost:5013/api',
});

// Request interceptor: automatically attach the JWT token to every request.
// This way we don't have to manually add the Authorization header everywhere.
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Response interceptor: if the server returns 401 (token expired/invalid),
// clear local storage and redirect to login.
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.clear();
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export default api;
