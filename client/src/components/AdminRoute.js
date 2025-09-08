import React from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

const AdminRoute = ({ children }) => {
  const { user, loading } = useAuth();

  if (loading) return null;

  if (!user) {
    return <Navigate to="/login" />;
  }

  const role = (user.userType || '').toString().toLowerCase();
  if (role !== 'admin') {
    return (
      <div className="container" style={{ maxWidth: 600, margin: '40px auto' }}>
        <h2>Access denied</h2>
        <p>You need admin privileges to view this page.</p>
      </div>
    );
  }

  return children;
};

export default AdminRoute;


