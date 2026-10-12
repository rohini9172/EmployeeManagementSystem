import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { getEmployees, searchEmployees, deleteEmployee } from '../services/employeeService';
import useApi from '../hooks/useApi';
import Toast from '../components/Toast';

export default function EmployeeListPage() {
  const { user } = useAuth();
  const [employees, setEmployees] = useState([]);
  const [term, setTerm] = useState('');
  const [toast, setToast] = useState(null);
  const { loading, error, execute, setError } = useApi();

  const fetchAll = async () => {
    const res = await execute(() => getEmployees());
    if (res) setEmployees(res.data);
  };

  useEffect(() => { fetchAll(); }, []);

  const handleSearch = async (e) => {
    e.preventDefault();
    if (!term.trim()) return fetchAll();
    const res = await execute(() => searchEmployees(term));
    if (res) setEmployees(res.data);
  };

  const handleDelete = async (id, name) => {
    if (!window.confirm(`Delete ${name}? This cannot be undone.`)) return;
    const res = await execute(() => deleteEmployee(id));
    if (res !== null) {
      setToast({ message: `${name} deleted successfully.`, type: 'success' });
      fetchAll();
    }
  };

  return (
    <div className="page">
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
        <h2>Employees</h2>
        {user?.role === 'Admin' && (
          <Link to="/employees/new" className="btn btn-primary">+ Add Employee</Link>
        )}
      </div>

      {/* Search bar */}
      <form onSubmit={handleSearch} style={{ display: 'flex', gap: '8px', marginBottom: '20px' }}>
        <input className="form-group" value={term}
          onChange={(e) => { setTerm(e.target.value); if (!e.target.value) fetchAll(); }}
          placeholder="Search by name, department, designation..."
          style={{ flex: 1, padding: '9px 12px', border: '1px solid #ccc', borderRadius: '5px', fontSize: '14px' }} />
        <button type="submit" className="btn btn-primary">Search</button>
        {term && (
          <button type="button" className="btn btn-secondary"
            onClick={() => { setTerm(''); fetchAll(); }}>Clear</button>
        )}
      </form>

      {error && <div className="alert alert-error">{error}</div>}

      {loading ? (
        <div className="spinner-wrap"><div className="spinner" /></div>
      ) : employees.length === 0 ? (
        <div className="alert alert-info">No employees found.</div>
      ) : (
        <table className="table">
          <thead>
            <tr>
              <th>#</th>
              <th>Name</th>
              <th>Email</th>
              <th>Department</th>
              <th>Designation</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {employees.map((emp, i) => (
              <tr key={emp.employeeId}>
                <td style={{ color: '#aaa' }}>{i + 1}</td>
                <td><strong>{emp.name}</strong></td>
                <td style={{ color: '#555' }}>{emp.email}</td>
                <td>{emp.department || '—'}</td>
                <td>{emp.designation || '—'}</td>
                <td>
                  <div style={{ display: 'flex', gap: '8px' }}>
                    <Link to={`/employees/${emp.employeeId}`} className="btn btn-primary"
                      style={{ padding: '4px 12px', fontSize: '13px', textDecoration: 'none' }}>View</Link>
                    {user?.role === 'Admin' && (
                      <>
                        <Link to={`/employees/${emp.employeeId}/edit`} className="btn btn-secondary"
                          style={{ padding: '4px 12px', fontSize: '13px', textDecoration: 'none' }}>Edit</Link>
                        <button className="btn btn-danger"
                          style={{ padding: '4px 12px', fontSize: '13px' }}
                          onClick={() => handleDelete(emp.employeeId, emp.name)}>Delete</button>
                      </>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {toast && <Toast message={toast.message} type={toast.type} onClose={() => setToast(null)} />}
    </div>
  );
}
