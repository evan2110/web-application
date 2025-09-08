import React, { useMemo, useState } from 'react';
import { useAuth } from '../contexts/AuthContext';

const AdminDashboard = () => {
  const { user } = useAuth();
  const [featureFlags, setFeatureFlags] = useState({ maintenanceMode: false, showBeta: false });
  const [auditLogs, setAuditLogs] = useState([]);

  const maskedEmail = useMemo(() => {
    if (!user?.email) return '';
    const [name, domain] = user.email.split('@');
    const safeName = name.length <= 2 ? name : `${name[0]}***${name[name.length - 1]}`;
    return `${safeName}@${domain}`;
  }, [user]);

  const addAudit = (action) => setAuditLogs((logs) => [{ ts: new Date().toISOString(), action }, ...logs].slice(0, 20));

  return (
    <div className="container" style={{ maxWidth: 900, margin: '24px auto' }}>
      <h2>Admin Dashboard</h2>
      <p>Signed in as: <strong>{maskedEmail}</strong></p>

      <div className="card" style={{ padding: 16, marginTop: 16 }}>
        <h3>Feature Toggles (local)</h3>
        <div style={{ display: 'flex', gap: 16, flexWrap: 'wrap' }}>
          <label>
            <input
              type="checkbox"
              checked={featureFlags.maintenanceMode}
              onChange={(e) => { setFeatureFlags({ ...featureFlags, maintenanceMode: e.target.checked }); addAudit(`Toggle maintenance=${e.target.checked}`); }}
            />
            <span style={{ marginLeft: 8 }}>Maintenance mode</span>
          </label>
          <label>
            <input
              type="checkbox"
              checked={featureFlags.showBeta}
              onChange={(e) => { setFeatureFlags({ ...featureFlags, showBeta: e.target.checked }); addAudit(`Toggle beta=${e.target.checked}`); }}
            />
            <span style={{ marginLeft: 8 }}>Show beta banner</span>
          </label>
        </div>
        {featureFlags.maintenanceMode && (
          <div className="alert alert-warning" style={{ marginTop: 12 }}>Maintenance banner active (demo only).</div>
        )}
        {featureFlags.showBeta && (
          <div className="alert alert-info" style={{ marginTop: 12 }}>Beta features visible (demo only).</div>
        )}
      </div>

      <div className="card" style={{ padding: 16, marginTop: 16 }}>
        <h3>Quick Admin Tools (client-only)</h3>
        <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
          <button className="btn" onClick={() => addAudit('Impersonate user (noop)')}>Impersonate user</button>
          <button className="btn" onClick={() => addAudit('Export CSV (generated locally)')}>Export CSV</button>
          <button className="btn" onClick={() => addAudit('Clear local storage') || localStorage.clear()}>Clear LocalStorage</button>
        </div>
      </div>

      <div className="card" style={{ padding: 16, marginTop: 16 }}>
        <h3>Audit Log (session)</h3>
        <ul style={{ listStyle: 'none', padding: 0, margin: 0 }}>
          {auditLogs.length === 0 && <li>No actions yet.</li>}
          {auditLogs.map((log, idx) => (
            <li key={idx} style={{ padding: '6px 0', borderBottom: '1px solid #eee' }}>
              <code>{log.ts}</code> — {log.action}
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
};

export default AdminDashboard;


