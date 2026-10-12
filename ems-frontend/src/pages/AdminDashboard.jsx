import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { getEmployees } from '../services/employeeService';
import { getActivitySummary } from '../services/activityService';

export default function AdminDashboard() {
  const { user } = useAuth();
  const [empCount, setEmpCount] = useState(0);
  const [deptCount, setDeptCount] = useState(0);
  const [summary, setSummary] = useState(null);

  useEffect(() => {
    getEmployees().then((res) => {
      const employees = res.data;
      const uniqueDepts = new Set(employees.map((e) => e.department).filter(Boolean));
      setEmpCount(employees.length);
      setDeptCount(uniqueDepts.size);
    }).catch(() => {});

    getActivitySummary().then((res) => setSummary(res.data)).catch(() => {});
  }, []);

  return (
    <div className="page">
      <h2>Welcome back, {user?.name}</h2>
      <p style={{ marginBottom: '24px' }}>
        <span className="badge badge-admin">Admin</span>
      </p>

      {/* Stat cards */}
      <div className="stat-grid">
        <div className="stat-card">
          <div className="stat-value">{empCount}</div>
          <div className="stat-label">Total Employees</div>
        </div>
        <div className="stat-card">
          <div className="stat-value">{deptCount}</div>
          <div className="stat-label">Departments</div>
        </div>
        <div className="stat-card">
          <div className="stat-value">{summary?.totalUniqueEmployeesViewed ?? '—'}</div>
          <div className="stat-label">Unique Profiles Viewed</div>
          <div style={{ fontSize: '11px', color: '#aaa', marginTop: '4px', fontFamily: 'monospace' }}>Redis SET</div>
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '20px', marginBottom: '24px' }}>

        {/* Popular employees from Redis Sorted Set */}
        <div className="card">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '14px' }}>
            <h3 style={{ fontSize: '1rem' }}>🏆 Most Viewed Employees</h3>
            <span style={{ fontSize: '11px', color: '#aaa', fontFamily: 'monospace' }}>Redis SORTED SET</span>
          </div>
          {!summary || summary.topEmployees.length === 0 ? (
            <p style={{ color: '#aaa', fontSize: '13px' }}>No views yet. View some employee profiles to see rankings.</p>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
              {summary.topEmployees.slice(0, 5).map((emp, i) => (
                <Link key={emp.employeeId} to={`/employees/${emp.employeeId}`}
                  style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '8px 10px', background: '#f8f9fa', borderRadius: '5px', textDecoration: 'none', color: '#222' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                    <span style={{ fontWeight: '700', color: i === 0 ? '#f39c12' : i === 1 ? '#95a5a6' : '#aaa', fontSize: '13px' }}>
                      #{i + 1}
                    </span>
                    <div>
                      <div style={{ fontWeight: '500' }}>{emp.name}</div>
                      <div style={{ fontSize: '11px', color: '#777' }}>{emp.department}</div>
                    </div>
                  </div>
                  <span style={{ fontSize: '12px', background: '#1a1a2e', color: '#fff', padding: '2px 8px', borderRadius: '10px' }}>
                    {emp.viewCount} views
                  </span>
                </Link>
              ))}
              <Link to="/activity" style={{ fontSize: '13px', color: '#1a1a2e', marginTop: '4px' }}>
                View full activity stats →
              </Link>
            </div>
          )}
        </div>

        {/* Quick actions */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
          <ActionCard to="/employees"      label="View All Employees"   color="#1a1a2e" />
          <ActionCard to="/employees/new"  label="Add New Employee"     color="#27ae60" />
          <ActionCard to="/activity"       label="Activity Statistics"  color="#2980b9" />
          <ActionCard to="/change-password" label="Change Password"     color="#8e44ad" />
        </div>

      </div>
    </div>
  );
}

function ActionCard({ to, label, color }) {
  return (
    <Link to={to} style={{
      background: color, color: '#fff', padding: '16px 20px', borderRadius: '8px',
      textDecoration: 'none', display: 'flex', alignItems: 'center',
      justifyContent: 'center', fontWeight: '600', fontSize: '14px',
      boxShadow: '0 2px 8px rgba(0,0,0,0.12)'
    }}>
      {label}
    </Link>
  );
}
