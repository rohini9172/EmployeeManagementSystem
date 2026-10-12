import { useEffect, useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { getEmployee } from '../services/employeeService';
import { getEmployeeStats } from '../services/activityService';

export default function EmployeeDetailPage() {
  const { id } = useParams();
  const { user } = useAuth();
  const navigate = useNavigate();
  const [emp, setEmp] = useState(null);
  const [stats, setStats] = useState(null); // Redis Hash data
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    setLoading(true);

    // Load employee details — this also triggers RecordViewAsync on the backend
    // which updates: Hash viewCount, Hash lastViewed, List recently-viewed,
    // Set unique-views, Sorted Set popularity ranking
    getEmployee(id)
      .then((res) => {
        setEmp(res.data);
        // After the view is recorded, fetch the updated Redis Hash stats
        // Small delay to let the backend fire-and-forget RecordViewAsync complete
        setTimeout(() => {
          getEmployeeStats(id)
            .then((r) => setStats(r.data))
            .catch(() => {}); // Stats are optional — don't break the page if Redis is down
        }, 300);
      })
      .catch(() => navigate('/employees'))
      .finally(() => setLoading(false));
  }, [id]);

  if (loading) return <div className="spinner-wrap"><div className="spinner" /></div>;
  if (!emp) return null;

  return (
    <div className="page" style={{ maxWidth: '640px' }}>
      <button onClick={() => navigate(-1)}
        style={{ background: 'none', border: 'none', cursor: 'pointer', color: '#1a1a2e', marginBottom: '16px', fontSize: '14px' }}>
        ← Back
      </button>

      <div className="card" style={{ marginBottom: '16px' }}>
        {/* Header */}
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '20px' }}>
          <div>
            <h2 style={{ marginBottom: '6px' }}>{emp.name}</h2>
            <span className="badge badge-employee">{emp.designation || 'Employee'}</span>
          </div>
          {user?.role === 'Admin' && (
            <Link to={`/employees/${emp.employeeId}/edit`} className="btn btn-secondary"
              style={{ textDecoration: 'none', fontSize: '13px' }}>Edit</Link>
          )}
        </div>

        {/* Employee details */}
        {[
          ['Email',        emp.email],
          ['Department',   emp.department   || '—'],
          ['Designation',  emp.designation  || '—'],
          ['Phone',        emp.phone        || '—'],
          ['Joined',       new Date(emp.createdAt).toLocaleDateString()],
          ['Last Updated', new Date(emp.updatedAt).toLocaleDateString()],
        ].map(([label, value]) => (
          <div key={label} style={{ display: 'flex', justifyContent: 'space-between', padding: '10px 0', borderBottom: '1px solid #f0f0f0' }}>
            <span style={{ color: '#777', fontWeight: '500' }}>{label}</span>
            <span>{value}</span>
          </div>
        ))}
      </div>

      {/* Redis Hash stats — shown to Admin only */}
      {user?.role === 'Admin' && stats && (
        <div className="card" style={{ background: '#f0f7ff', border: '1px solid #bee3f8' }}>
          <h3 style={{ fontSize: '0.95rem', marginBottom: '12px', color: '#1a5276' }}>
            📊 Redis Activity Stats
            <span style={{ fontSize: '11px', color: '#aaa', marginLeft: '8px', fontWeight: 'normal' }}>
              from Redis Hash · activity:employee:{id}
            </span>
          </h3>
          <div style={{ display: 'flex', gap: '24px' }}>
            <div>
              <div style={{ fontSize: '2rem', fontWeight: '700', color: '#1a1a2e' }}>
                {stats.viewCount}
              </div>
              <div style={{ fontSize: '12px', color: '#777' }}>Total Views</div>
              <div style={{ fontSize: '11px', color: '#aaa', fontFamily: 'monospace' }}>HGET viewCount</div>
            </div>
            <div>
              <div style={{ fontSize: '1rem', fontWeight: '600', color: '#1a1a2e' }}>
                {stats.lastViewed === 'Never'
                  ? 'Never'
                  : new Date(stats.lastViewed).toLocaleString()}
              </div>
              <div style={{ fontSize: '12px', color: '#777' }}>Last Viewed</div>
              <div style={{ fontSize: '11px', color: '#aaa', fontFamily: 'monospace' }}>HGET lastViewed</div>
            </div>
          </div>
        </div>
      )}

      {/* View recording notice for non-admin */}
      {user?.role !== 'Admin' && (
        <div className="alert alert-info" style={{ fontSize: '13px' }}>
          ✓ This view has been recorded in Redis (recently viewed list + popularity ranking)
        </div>
      )}
    </div>
  );
}
