import { useEffect, useState } from 'react';
import { getMyProfile, updateEmployee } from '../services/employeeService';
import useApi from '../hooks/useApi';
import Toast from '../components/Toast';

export default function MyProfilePage() {
  const [profile, setProfile] = useState(null);
  const [form, setForm] = useState({ phone: '', department: '', designation: '' });
  const [toast, setToast] = useState(null);
  const { loading, error, execute } = useApi();

  useEffect(() => {
    getMyProfile().then((res) => {
      setProfile(res.data);
      const { phone, department, designation } = res.data;
      setForm({ phone, department, designation });
    }).catch(() => {});
  }, []);

  const handleSubmit = async (e) => {
    e.preventDefault();
    const res = await execute(() => updateEmployee(profile.employeeId, form));
    if (res) setToast({ message: 'Profile updated!', type: 'success' });
  };

  if (!profile) return <div className="spinner-wrap"><div className="spinner" /></div>;

  return (
    <div className="page" style={{ maxWidth: '560px' }}>
      <h2>My Profile</h2>

      {/* Read-only info */}
      <div className="card" style={{ marginBottom: '20px' }}>
        <div style={{ display: 'flex', gap: '24px' }}>
          <div>
            <p style={{ color: '#777', fontSize: '13px' }}>Name</p>
            <p style={{ fontWeight: '600' }}>{profile.name}</p>
          </div>
          <div>
            <p style={{ color: '#777', fontSize: '13px' }}>Email</p>
            <p style={{ fontWeight: '600' }}>{profile.email}</p>
          </div>
        </div>
      </div>

      {/* Editable fields */}
      <div className="card">
        <h3 style={{ marginBottom: '16px', fontSize: '1rem' }}>Update Details</h3>
        {error && <div className="alert alert-error">{error}</div>}
        <form onSubmit={handleSubmit}>
          {[
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
          <button type="submit" className="btn btn-primary" disabled={loading}>
            {loading ? 'Saving...' : 'Update Profile'}
          </button>
        </form>
      </div>

      {toast && <Toast message={toast.message} type={toast.type} onClose={() => setToast(null)} />}
    </div>
  );
}
