import React from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

const Navbar = () => {
  const { user, logout } = useAuth();

  return (
    <nav className="navbar">
      <div className="container">
        <Link to="/" className="navbar-brand">
          React App
        </Link>
        
        <ul className="navbar-nav">
          {user ? (
            <>
              <li>
                <Link to="/home">Home</Link>
              </li>
              <li>
                <span>Welcome, {user.name}!</span>
              </li>
              <li>
                <button 
                  className="btn btn-secondary" 
                  onClick={() => logout()}
                >
                  Logout
                </button>
              </li>
            </>
          ) : (
            <>
              <li>
                <Link to="/login">Login</Link>
              </li>
              <li>
                <Link to="/register">Register</Link>
              </li>
            </>
          )}
        </ul>
      </div>
    </nav>
  );
};

export default Navbar;