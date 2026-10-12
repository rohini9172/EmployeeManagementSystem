import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider, useAuth } from './context/AuthContext';
import ProtectedRoute from './components/ProtectedRoute';
import Navbar from './components/Navbar';

import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import AdminDashboard from './pages/AdminDashboard';
import EmployeeDashboard from './pages/EmployeeDashboard';
import EmployeeListPage from './pages/EmployeeListPage';
import EmployeeDetailPage from './pages/EmployeeDetailPage';
import AddEmployeePage from './pages/AddEmployeePage';
import EditEmployeePage from './pages/EditEmployeePage';
import MyProfilePage from './pages/MyProfilePage';
import ChangePasswordPage from './pages/ChangePasswordPage';
import ActivityPage from './pages/ActivityPage';
import RecentlyViewedPage from './pages/RecentlyViewedPage';

function AppRoutes() {
  const { user } = useAuth();

  return (
    <>
      {/* Navbar only shows when logged in */}
      {user && <Navbar />}

      <Routes>
        {/* ── Public ──────────────────────────────────────────── */}
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />

        {/* ── Admin only ──────────────────────────────────────── */}
        <Route path="/admin" element={
          <ProtectedRoute role="Admin"><AdminDashboard /></ProtectedRoute>} />
        <Route path="/employees/new" element={
          <ProtectedRoute role="Admin"><AddEmployeePage /></ProtectedRoute>} />
        <Route path="/activity" element={
          <ProtectedRoute role="Admin"><ActivityPage /></ProtectedRoute>} />

        {/* ── Shared (Admin + Employee) ────────────────────────── */}
        <Route path="/employees" element={
          <ProtectedRoute><EmployeeListPage /></ProtectedRoute>} />
        <Route path="/employees/:id" element={
          <ProtectedRoute><EmployeeDetailPage /></ProtectedRoute>} />
        <Route path="/employees/:id/edit" element={
          <ProtectedRoute><EditEmployeePage /></ProtectedRoute>} />
        <Route path="/change-password" element={
          <ProtectedRoute><ChangePasswordPage /></ProtectedRoute>} />

        {/* ── Employee only ────────────────────────────────────── */}
        <Route path="/dashboard" element={
          <ProtectedRoute><EmployeeDashboard /></ProtectedRoute>} />
        <Route path="/profile" element={
          <ProtectedRoute><MyProfilePage /></ProtectedRoute>} />
        <Route path="/recently-viewed" element={
          <ProtectedRoute><RecentlyViewedPage /></ProtectedRoute>} />

        {/* ── Default redirect ─────────────────────────────────── */}
        <Route path="/" element={
          <Navigate to={user ? (user.role === 'Admin' ? '/admin' : '/dashboard') : '/login'} />
        } />
        <Route path="*" element={<Navigate to="/" />} />
      </Routes>
    </>
  );
}

// BrowserRouter must wrap AuthProvider because AuthProvider uses
// useNavigate internally via the Axios interceptor redirect
export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <AppRoutes />
      </AuthProvider>
    </BrowserRouter>
  );
}
