import React, { useState, useEffect } from 'react';
import { useAuth } from '../contexts/AuthContext';
import { decodeJWT } from '../utils/jwtUtils';
import UserProfile from '../components/UserProfile';
import TodoList from '../components/TodoList';
import Counter from '../components/Counter';

const Home = () => {
  const { user, getAccessToken } = useAuth();
  const [activeTab, setActiveTab] = useState('profile');
  const [tokenInfo, setTokenInfo] = useState(null);

  useEffect(() => {
    document.title = 'Home - React App';
    
    // Debug: Decode token to show token information
    const accessToken = getAccessToken();
    if (accessToken) {
      const decoded = decodeJWT(accessToken);
      setTokenInfo(decoded);
      console.log('Decoded token:', decoded);
    }
  }, [getAccessToken]);

  return (
    <div className="container">
      <div className="text-center mb-4">
        <h1>Welcome to React App!</h1>
        <p>This is a comprehensive React application showcasing various components and hooks.</p>
        {user && (
          <div className="alert alert-info mt-3" role="alert">
            <strong>Welcome back, {user.email}!</strong>
            <br />
            <small>User Type: <span className="badge bg-primary">{user.userType || 'User'}</span></small>
          </div>
        )}
      </div>

      {/* Tab Navigation */}
      <div className="tab-navigation mb-4">
        <button 
          className={`tab-btn tab-btn-profile ${activeTab === 'profile' ? 'active' : ''}`}
          onClick={() => setActiveTab('profile')}
        >
          <span className="tab-icon">👤</span>
          User Profile
        </button>
        <button 
          className={`tab-btn tab-btn-todos ${activeTab === 'todos' ? 'active' : ''}`}
          onClick={() => setActiveTab('todos')}
        >
          <span className="tab-icon">📝</span>
          Todo List
        </button>
        <button 
          className={`tab-btn tab-btn-counter ${activeTab === 'counter' ? 'active' : ''}`}
          onClick={() => setActiveTab('counter')}
        >
          <span className="tab-icon">🔢</span>
          Counter
        </button>
        {(user?.userType || '').toLowerCase() === 'admin' && (
          <button 
            className={`tab-btn tab-btn-admin ${activeTab === 'admin' ? 'active' : ''}`}
            onClick={() => setActiveTab('admin')}
          >
            <span className="tab-icon">🛠️</span>
            Admin Tools
          </button>
        )}
      </div>

      {/* Tab Content */}
      <div className="tab-content">
        {activeTab === 'profile' && <UserProfile user={user} />}
        {activeTab === 'todos' && <TodoList />}
        {activeTab === 'counter' && <Counter />}
        {activeTab === 'admin' && (user?.userType || '').toLowerCase() === 'admin' && (
          <div className="card" style={{ padding: 16 }}>
            <h3>Quick Admin Tools (no API)</h3>
            <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
              <button className="btn" onClick={() => alert('Opened system health (client-only)')}>System Health</button>
              <button className="btn" onClick={() => alert('Cleared cache (client-only)')}>Clear Cache</button>
              <button className="btn" onClick={() => alert('Simulated user sync (client-only)')}>Sync Users</button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default Home;
