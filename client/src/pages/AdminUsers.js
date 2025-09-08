import React, { useEffect, useMemo, useState } from 'react';
import { useAuth } from '../contexts/AuthContext';

const AdminUsers = () => {
  const { user, getAccessToken } = useAuth();
  const [rows, setRows] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [q, setQ] = useState('');

  useEffect(() => {
    const load = async () => {
      try {
        const token = getAccessToken();
        const base = process.env.REACT_APP_API_BASE_URL || '';
        const url = `${base}/api/Users`.replace('//api', '/api');
        const res = await fetch(url, {
          headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
          }
        });
        if (!res.ok) {
          const data = await res.json().catch(() => ({}));
          throw new Error(data.message || `Request failed (${res.status})`);
        }
        const data = await res.json();
        setRows(Array.isArray(data) ? data : []);
      } catch (e) {
        setError(e.message || 'Failed to load users');
      } finally {
        setLoading(false);
      }
    };
    load();
  }, [getAccessToken]);

  const filtered = useMemo(() => {
    if (!q) return rows;
    const s = q.toLowerCase();
    return rows.filter(r => (r.email || '').toLowerCase().includes(s));
  }, [rows, q]);

  if ((user?.userType || '').toLowerCase() !== 'admin') {
    return <div className="container" style={{ maxWidth: 600, margin: '24px auto' }}>Access denied.</div>;
  }

  return (
    <div className="container" style={{ maxWidth: 900, margin: '24px auto' }}>
      <h2>Users</h2>
      <div style={{ margin: '12px 0' }}>
        <input
          placeholder="Search by email..."
          value={q}
          onChange={(e) => setQ(e.target.value)}
          style={{ width: '100%', padding: 8 }}
        />
      </div>
      {loading && <p>Loading...</p>}
      {error && <p style={{ color: 'crimson' }}>{error}</p>}
      {!loading && !error && (
        <div className="card" style={{ padding: 0 }}>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr>
                <th style={{ textAlign: 'left', padding: 8, borderBottom: '1px solid #eee' }}>ID</th>
                <th style={{ textAlign: 'left', padding: 8, borderBottom: '1px solid #eee' }}>Email</th>
                <th style={{ textAlign: 'left', padding: 8, borderBottom: '1px solid #eee' }}>Role</th>
                <th style={{ textAlign: 'left', padding: 8, borderBottom: '1px solid #eee' }}>Created</th>
                <th style={{ textAlign: 'left', padding: 8, borderBottom: '1px solid #eee' }}>Verified</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map(u => (
                <tr key={u.id}>
                  <td style={{ padding: 8, borderBottom: '1px solid #f3f3f3' }}>{u.id}</td>
                  <td style={{ padding: 8, borderBottom: '1px solid #f3f3f3' }}>{u.email}</td>
                  <td style={{ padding: 8, borderBottom: '1px solid #f3f3f3' }}>{u.userType || 'User'}</td>
                  <td style={{ padding: 8, borderBottom: '1px solid #f3f3f3' }}>{u.createdAt ? new Date(u.createdAt).toLocaleString() : '-'}</td>
                  <td style={{ padding: 8, borderBottom: '1px solid #f3f3f3' }}>{u.confirmedAt ? new Date(u.confirmedAt).toLocaleString() : 'No'}</td>
                </tr>
              ))}
            </tbody>
          </table>
          {filtered.length === 0 && <div style={{ padding: 12 }}>No users.</div>}
        </div>
      )}
    </div>
  );
};

export default AdminUsers;


