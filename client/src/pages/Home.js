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
      </div>

      {/* Tab Navigation */}
      <div className="tab-navigation mb-4">
        <button 
          className={`tab-btn ${activeTab === 'profile' ? 'active' : ''}`}
          onClick={() => setActiveTab('profile')}
        >
          User Profile
        </button>
        <button 
          className={`tab-btn ${activeTab === 'todos' ? 'active' : ''}`}
          onClick={() => setActiveTab('todos')}
        >
          Todo List
        </button>
        <button 
          className={`tab-btn ${activeTab === 'counter' ? 'active' : ''}`}
          onClick={() => setActiveTab('counter')}
        >
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
