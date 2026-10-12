import { useEffect, useState } from 'react';
import { getActivitySummary, getPopularEmployees, getRecentlyViewed } from '../services/activityService';
import useApi from '../hooks/useApi';
import { useAuth } from '../context/AuthContext';

export default function ActivityPage() {
  const { user } = useAuth();
  const [summary, setSummary] = useState(null);
  const [popular, setPopular] = useState([]);
  const [recent, setRecent] = useState([]);
  const { loading, error, execute } = useApi();

  useEffect(() => {
    const load = async () => {
      // Load all three in parallel for speed
      const [summaryRes, popularRes, recentRes] = await Promise.all([
        execute(() => getActivitySummary()),
        execute(() => getPopularEmployees()),
        execute(() => getRecentlyViewed()),
      ]);
      if (summaryRes) setSummary(summaryRes.data);
      if (popularRes) setPopular(popularRes.data);
      if (recentRes) setRecent(recentRes.data);
    };
    load();
  }, []);

  return (
    <div className="page">
      <h2>Activity Statistics</h2>
      <p style={{ color: '#777', marginBottom: '24px', fontSize: '14px' }}>
        Powered by Redis — Hash, List, Set, Sorted Set
      </p>

      {error && <div className="alert alert-error">{error}</div>}
      {loading && <div className="spinner-wrap"><div className="spinner" /></div>}

      {/* Summary stats from Redis Set */}
      {summary && (
        <div className="stat-grid" style={{ marginBottom: '28px' }}>
          <div className="stat-card">
            <div className="stat-value">{summary.totalUniqueEmployeesViewed}</div>
            <div className="stat-label">Unique Employees Viewed</div>
            <div style={{ fontSize: '11px', color: '#aaa', marginTop: '4px' }}>Redis SET (SCARD)</div>
          </div>
          <div className="stat-card">
            <div className="stat-value">{summary.topEmployees?.length ?? 0}</div>
            <div className="stat-label">Ranked Employees</div>
            <div style={{ fontSize: '11px', color: '#aaa', marginTop: '4px' }}>Redis SORTED SET</div>
          </div>
        </div>
      )}

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '24px' }}>

        {/* Popular employees from Redis Sorted Set */}
        <div className="card">
          <h3 style={{ marginBottom: '16px', fontSize: '1rem' }}>
            🏆 Most Viewed Employees
            <span style={{ fontSize: '11px', color: '#aaa', marginLeft: '8px' }}>Redis SORTED SET</span>
          </h3>
          {popular.length === 0 ? (
            <p style={{ color: '#aaa', fontSize: '14px' }}>No views recorded yet. View some employee profiles!</p>
          ) : (
            <table className="table">
              <thead>
                <tr><th>Rank</th><th>Name</th><th>Department</th><th>Views</th></tr>
              </thead>
              <tbody>
                {popular.map((emp, i) => (
                  <tr key={emp.employeeId}>
                    <td>
                      <span style={{ fontWeight: '700', color: i === 0 ? '#f39c12' : i === 1 ? '#95a5a6' : '#cd6133' }}>
                        #{i + 1}
                      </span>
                    </td>
                    <td>{emp.name}</td>
                    <td>{emp.department}</td>
                    <td><strong>{emp.viewCount}</strong></td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>

        {/* Recently viewed from Redis List */}
        <div className="card">
          <h3 style={{ marginBottom: '16px', fontSize: '1rem' }}>
            🕐 Recently Viewed (Your History)
            <span style={{ fontSize: '11px', color: '#aaa', marginLeft: '8px' }}>Redis LIST</span>
          </h3>
          {recent.length === 0 ? (
            <p style={{ color: '#aaa', fontSize: '14px' }}>No recent views. Browse some employee profiles!</p>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
              {recent.map((emp) => (
                <div key={emp.employeeId} style={{
                  display: 'flex', justifyContent: 'space-between', alignItems: 'center',
                  padding: '10px 12px', background: '#f8f9fa', borderRadius: '6px'
                }}>
                  <div>
                    <div style={{ fontWeight: '600' }}>{emp.name}</div>
                    <div style={{ fontSize: '12px', color: '#777' }}>{emp.designation}</div>
                  </div>
                  <span style={{ fontSize: '12px', color: '#aaa' }}>{emp.department}</span>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      {/* Redis data structure explanation for interview */}
      <div className="card" style={{ marginTop: '24px' }}>
        <h3 style={{ marginBottom: '12px', fontSize: '1rem' }}>Redis Data Structures Used</h3>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '12px' }}>
          {[
            { type: 'String', key: 'employees:all', use: 'Cache employee list' },
            { type: 'Hash',   key: 'activity:employee:{id}', use: 'View count + last viewed' },
            { type: 'List',   key: 'activity:recent:{userId}', use: 'Last 5 viewed per user' },
            { type: 'Set',    key: 'activity:unique-views', use: 'Unique employee IDs viewed' },
            { type: 'Sorted Set', key: 'activity:popular', use: 'Employee view rankings' },
          ].map(({ type, key, use }) => (
            <div key={type} style={{ padding: '12px', background: '#f0f2f5', borderRadius: '6px' }}>
              <div style={{ fontWeight: '700', color: '#1a1a2e', marginBottom: '4px' }}>{type}</div>
              <div style={{ fontSize: '11px', fontFamily: 'monospace', color: '#555', marginBottom: '4px' }}>{key}</div>
              <div style={{ fontSize: '12px', color: '#777' }}>{use}</div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
