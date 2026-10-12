import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { getMyProfile } from '../services/employeeService';
import { getRecentlyViewed, getPopularEmployees } from '../services/activityService';

export default function EmployeeDashboard() {
  const { user } = useAuth();
  const [profile, setProfile] = useState(null);
  const [recent, setRecent] = useState([]);
  const [popular, setPopular] = useState([]);

  useEffect(() => {
    // Load all three in parallel
    getMyProfile().then((res) => setProfile(res.data)).catch(() => {});
    getRecentlyViewed().then((res) => setRecent(res.data)).catch(() => {});
    getPopularEmployees().then((res) => setPopular(res.data)).catch(() => {});
  }, []);

  return (
    <div className="page">
      <h2>Welcome, {user?.name}</h2>
      <p style={{ marginBottom: '24px' }}>
        <span className="badge badge-employee">Employee</span>
      </p>

      {/* Profile stat cards */}
      {profile && (
        <div className="stat-grid">
          <div className="stat-card">
            <div className="stat-value" style={{ fontSize: '1.1rem' }}>{profile.department || '—'}</div>
            <div className="stat-label">Department</div>
          </div>
          <div className="stat-card">
            <div className="stat-value" style={{ fontSize: '1.1rem' }}>{profile.designation || '—'}</div>
            <div className="stat-label">Designation</div>
          </div>
          <div className="stat-card">
            <div className="stat-value" style={{ fontSize: '1.1rem' }}>{profile.phone || '—'}</div>
            <div className="stat-label">Phone</div>
          </div>
        </div>
      )}

      {/* Quick actions */}
      <div style={{ display: 'flex', gap: '12px', flexWrap: 'wrap', marginBottom: '28px' }}>
        <Link to="/employees" className="btn btn-primary" style={{ textDecoration: 'none' }}>Browse Employees</Link>
        <Link to="/profile" className="btn btn-secondary" style={{ textDecoration: 'none' }}>Edit My Profile</Link>
        <Link to="/recently-viewed" className="btn btn-secondary" style={{ textDecoration: 'none' }}>Recently Viewed</Link>
        <Link to="/change-password" className="btn btn-secondary" style={{ textDecoration: 'none' }}>Change Password</Link>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '20px' }}>

        {/* Recently Viewed — Redis List */}
        <div className="card">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '14px' }}>
            <h3 style={{ fontSize: '1rem' }}>🕐 Recently Viewed</h3>
            <span style={{ fontSize: '11px', color: '#aaa', fontFamily: 'monospace' }}>Redis LIST</span>
          </div>
          {recent.length === 0 ? (
            <p style={{ color: '#aaa', fontSize: '13px' }}>
              No history yet. <Link to="/employees">Browse employees</Link> to start tracking.
            </p>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
              {recent.map((emp) => (
                <Link key={emp.employeeId} to={`/employees/${emp.employeeId}`}
                  style={{ display: 'flex', justifyContent: 'space-between', padding: '8px 10px', background: '#f8f9fa', borderRadius: '5px', textDecoration: 'none', color: '#222' }}>
                  <span style={{ fontWeight: '500' }}>{emp.name}</span>
                  <span style={{ fontSize: '12px', color: '#777' }}>{emp.department}</span>
                </Link>
              ))}
              <Link to="/recently-viewed" style={{ fontSize: '13px', color: '#1a1a2e', marginTop: '4px' }}>
                View full history →
              </Link>
            </div>
          )}
        </div>

        {/* Popular Employees — Redis Sorted Set */}
        <div className="card">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '14px' }}>
            <h3 style={{ fontSize: '1rem' }}>🏆 Most Viewed</h3>
            <span style={{ fontSize: '11px', color: '#aaa', fontFamily: 'monospace' }}>Redis SORTED SET</span>
          </div>
          {popular.length === 0 ? (
            <p style={{ color: '#aaa', fontSize: '13px' }}>No views recorded yet.</p>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
              {popular.map((emp, i) => (
                <Link key={emp.employeeId} to={`/employees/${emp.employeeId}`}
                  style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '8px 10px', background: '#f8f9fa', borderRadius: '5px', textDecoration: 'none', color: '#222' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                    <span style={{ fontWeight: '700', color: i === 0 ? '#f39c12' : '#aaa', fontSize: '13px' }}>#{i + 1}</span>
                    <span style={{ fontWeight: '500' }}>{emp.name}</span>
                  </div>
                  <span style={{ fontSize: '12px', background: '#1a1a2e', color: '#fff', padding: '2px 8px', borderRadius: '10px' }}>
                    {emp.viewCount} views
                  </span>
                </Link>
              ))}
            </div>
          )}
        </div>

      </div>
    </div>
  );
}
