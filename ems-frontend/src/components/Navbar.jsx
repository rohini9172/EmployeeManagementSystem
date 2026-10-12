import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export default function Navbar() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <nav style={styles.nav}>
      <span style={styles.brand}>EMS</span>
      <div style={styles.links}>
        {user?.role === 'Admin' && (
          <>
            <Link to="/admin" style={styles.link}>Dashboard</Link>
            <Link to="/employees" style={styles.link}>Employees</Link>
            <Link to="/employees/new" style={styles.link}>Add Employee</Link>
            <Link to="/activity" style={styles.link}>Activity</Link>
          </>
        )}
        {user?.role === 'Employee' && (
          <>
            <Link to="/dashboard" style={styles.link}>Dashboard</Link>
            <Link to="/employees" style={styles.link}>Employees</Link>
            <Link to="/recently-viewed" style={styles.link}>Recently Viewed</Link>
            <Link to="/profile" style={styles.link}>My Profile</Link>
            <Link to="/change-password" style={styles.link}>Change Password</Link>
          </>
        )}
        {user?.role === 'Admin' && (
          <Link to="/change-password" style={styles.link}>Change Password</Link>
        )}
        <button onClick={handleLogout} style={styles.btn}>Logout</button>
      </div>
    </nav>
  );
}

const styles = {
  nav: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '12px 24px', background: '#1a1a2e', color: '#fff' },
  brand: { fontWeight: 'bold', fontSize: '1.2rem' },
  links: { display: 'flex', gap: '16px', alignItems: 'center' },
  link: { color: '#eee', textDecoration: 'none' },
  btn: { background: '#e94560', color: '#fff', border: 'none', padding: '6px 14px', borderRadius: '4px', cursor: 'pointer' },
};
