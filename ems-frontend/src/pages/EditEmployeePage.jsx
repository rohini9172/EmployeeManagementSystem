import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { getEmployee, updateEmployee } from '../services/employeeService';
import useApi from '../hooks/useApi';
import Toast from '../components/Toast';

export default function EditEmployeePage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { loading, error, execute } = useApi();
  const [form, setForm] = useState({ name: '', department: '', designation: '', phone: '' });
  const [toast, setToast] = useState(null);

  useEffect(() => {
    getEmployee(id).then((res) => {
      const { name, department, designation, phone } = res.data;
      setForm({ name, department, designation, phone });
    }).catch(() => navigate('/employees'));
  }, [id]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    const res = await execute(() => updateEmployee(id, form));
    if (res) {
      setToast({ message: 'Employee updated successfully!', type: 'success' });
      // Navigate back after a short delay so the toast is visible
      setTimeout(() => navigate('/employees'), 1200);
    }
  };

  return (
    <div className="page" style={{ maxWidth: '520px' }}>
      <h2>Edit Employee</h2>

      {error && <div className="alert alert-error">{error}</div>}

      <div className="card">
        <form onSubmit={handleSubmit}>
          {[
            { key: 'name',        label: 'Full Name'   },
            { key: 'department',  label: 'Department'  },
            { key: 'designation', label: 'Designation' },
            { key: 'phone',       label: 'Phone'       },
          ].map(({ key, label }) => (
            <div className="form-group" key={key}>
              <label>{label}</label>
              <input value={form[key]}
                onChange={(e) => setForm({ ...form, [key]: e.target.value })} />
            </div>
          ))}
          <div style={{ display: 'flex', gap: '10px', marginTop: '8px' }}>
            <button type="submit" className="btn btn-primary" disabled={loading}>
              {loading ? 'Saving...' : 'Save Changes'}
            </button>
            <button type="button" className="btn btn-secondary" onClick={() => navigate(-1)}>
              Cancel
            </button>
          </div>
        </form>
      </div>

      {toast && <Toast message={toast.message} type={toast.type} onClose={() => setToast(null)} />}
    </div>
  );
}
