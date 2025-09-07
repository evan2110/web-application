import React, { useState, useEffect } from 'react';
import { useAuth } from '../contexts/AuthContext';
import UserProfile from '../components/UserProfile';
import TodoList from '../components/TodoList';
import Counter from '../components/Counter';

const Home = () => {
  const { user } = useAuth();
  const [activeTab, setActiveTab] = useState('profile');

  useEffect(() => {
    document.title = 'Home - React App';
  }, []);

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
      </div>

      {/* Tab Content */}
      <div className="tab-content">
        {activeTab === 'profile' && <UserProfile user={user} />}
        {activeTab === 'todos' && <TodoList />}
        {activeTab === 'counter' && <Counter />}
      </div>
    </div>
  );
};

export default Home;
