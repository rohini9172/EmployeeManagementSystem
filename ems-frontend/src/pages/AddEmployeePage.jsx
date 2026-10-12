import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { createEmployee } from '../services/employeeService';
import useApi from '../hooks/useApi';

const FIELDS = [
  { key: 'name',        label: 'Full Name',   type: 'text'     },
  { key: 'email',       label: 'Email',        type: 'email'    },
  { key: 'password',    label: 'Password',     type: 'password' },
  { key: 'department',  label: 'Department',   type: 'text'     },
  { key: 'designation', label: 'Designation',  type: 'text'     },
  { key: 'phone',       label: 'Phone',        type: 'text'     },
];

export default function AddEmployeePage() {
  const navigate = useNavigate();
  const { loading, error, execute } = useApi();
  const [form, setForm] = useState({ name: '', email: '', password: '', department: '', designation: '', phone: '' });

  const handleSubmit = async (e) => {
    e.preventDefault();
    const res = await execute(() => createEmployee(form));
    // execute() returns null on error (error state is set automatically by useApi)
    if (res) navigate('/employees');
  };

  return (
    <div className="page" style={{ maxWidth: '520px' }}>
      <h2>Add New Employee</h2>

      {error && <div className="alert alert-error">{error}</div>}

      <div className="card">
        <form onSubmit={handleSubmit}>
          {FIELDS.map(({ key, label, type }) => (
            <div className="form-group" key={key}>
              <label>{label}</label>
              <input type={type} value={form[key]} required
                onChange={(e) => setForm({ ...form, [key]: e.target.value })} />
            </div>
          ))}
          <div style={{ display: 'flex', gap: '10px', marginTop: '8px' }}>
            <button type="submit" className="btn btn-primary" disabled={loading}>
              {loading ? 'Creating...' : 'Create Employee'}
            </button>
            <button type="button" className="btn btn-secondary" onClick={() => navigate('/employees')}>
              Cancel
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
