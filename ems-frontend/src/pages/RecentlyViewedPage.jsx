import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { getRecentlyViewed } from '../services/activityService';
import useApi from '../hooks/useApi';

export default function RecentlyViewedPage() {
  const [recent, setRecent] = useState([]);
  const { loading, error, execute } = useApi();

  useEffect(() => {
    execute(() => getRecentlyViewed()).then((res) => {
      if (res) setRecent(res.data);
    });
  }, []);

  return (
    <div className="page" style={{ maxWidth: '640px' }}>
      <h2>Recently Viewed Employees</h2>
      <p style={{ color: '#777', fontSize: '13px', marginBottom: '20px' }}>
        Your last 5 viewed profiles · stored in{' '}
        <code style={{ background: '#f0f0f0', padding: '2px 6px', borderRadius: '3px' }}>
          Redis LIST
        </code>
        {' '}· key:{' '}
        <code style={{ background: '#f0f0f0', padding: '2px 6px', borderRadius: '3px' }}>
          activity:recent:&#123;userId&#125;
        </code>
      </p>

      {error && <div className="alert alert-error">{error}</div>}

      {loading ? (
        <div className="spinner-wrap"><div className="spinner" /></div>
      ) : recent.length === 0 ? (
        <div className="card" style={{ textAlign: 'center', padding: '40px' }}>
          <p style={{ color: '#aaa', marginBottom: '16px' }}>No recently viewed employees yet.</p>
          <Link to="/employees" className="btn btn-primary">Browse Employees</Link>
        </div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
          {recent.map((emp, index) => (
            <div key={emp.employeeId} className="card"
              style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '16px 20px' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
                {/* Position badge — most recent is #1 */}
                <div style={{
                  width: '32px', height: '32px', borderRadius: '50%',
                  background: index === 0 ? '#1a1a2e' : '#e0e0e0',
                  color: index === 0 ? '#fff' : '#555',
                  display: 'flex', alignItems: 'center', justifyContent: 'center',
                  fontWeight: '700', fontSize: '13px', flexShrink: 0
                }}>
                  {index + 1}
                </div>
                <div>
                  <div style={{ fontWeight: '600' }}>{emp.name}</div>
                  <div style={{ fontSize: '13px', color: '#777' }}>
                    {emp.designation || '—'} · {emp.department || '—'}
                  </div>
                </div>
              </div>
              <Link to={`/employees/${emp.employeeId}`} className="btn btn-primary"
                style={{ textDecoration: 'none', fontSize: '13px', padding: '6px 14px' }}>
                View
              </Link>
            </div>
          ))}
        </div>
      )}

      <div className="alert alert-info" style={{ marginTop: '20px', fontSize: '13px' }}>
        <strong>How it works:</strong> Each time you view an employee profile, their ID is pushed
        to the front of your personal Redis List. Duplicates are removed before pushing so the
        same employee never appears twice. The list is trimmed to 5 items automatically.
      </div>
    </div>
  );
}
