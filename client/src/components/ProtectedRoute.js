import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

const ProtectedRoute = ({ children }) => {
  const { user, loading, tokenRefreshing, checkAndRefreshToken } = useAuth();
  const navigate = useNavigate();
  const [isCheckingToken, setIsCheckingToken] = useState(false);

  // Check token when loading is complete
  useEffect(() => {
    const checkTokenAndRedirect = async () => {
      if (!loading && !user) {
        // No user, check if we can refresh token
        setIsCheckingToken(true);
        const isAuthenticated = await checkAndRefreshToken();
        setIsCheckingToken(false);
        
        if (!isAuthenticated) {
          // Still no user after refresh attempt, redirect to login
          const timer = setTimeout(() => {
            navigate('/login');
          }, 3000);
          
          return () => clearTimeout(timer);
        }
      }
    };

    checkTokenAndRedirect();
  }, [loading, checkAndRefreshToken, navigate]);

  // Handle redirect when user becomes null after being authenticated
  useEffect(() => {
    if (!loading && !user && !isCheckingToken && !tokenRefreshing) {
      const timer = setTimeout(() => {
        navigate('/login');
      }, 3000);
      
      return () => clearTimeout(timer);
    }
  }, [user, loading, isCheckingToken, tokenRefreshing, navigate]);

  // Show loading spinner while checking authentication or refreshing token
  if (loading || tokenRefreshing || isCheckingToken) {
    return (
      <div className="container">
        <div className="d-flex justify-content-center align-items-center min-vh-100">
          <div className="text-center">
            <div className="spinner-border text-primary" role="status">
              <span className="visually-hidden">Loading...</span>
            </div>
            <p className="mt-3">
              {tokenRefreshing ? 'Refreshing token...' : 
               isCheckingToken ? 'Checking authentication...' : 
               'Loading...'}
            </p>
          </div>
        </div>
      </div>
    );
  }

  // If user is not authenticated, show error message
  if (!user) {
    return (
      <div className="container">
        <div className="d-flex justify-content-center align-items-center min-vh-100">
          <div className="card" style={{ width: '100%', maxWidth: '500px' }}>
            <div className="text-center mb-4">
              <div className="text-danger mb-3">
                <i className="fas fa-exclamation-triangle" style={{ fontSize: '4rem' }}></i>
              </div>
              <h2 className="text-danger mb-3">Access Denied</h2>
              <p className="text-muted mb-4">You need to login to access this page</p>
              
              <div className="alert alert-warning" role="alert">
                <strong>Authentication Required!</strong><br/>
                Please login with your credentials to continue.
              </div>
            </div>
            
            <div className="text-center">
              <button 
                onClick={() => navigate('/login')}
                className="btn btn-primary btn-lg"
                style={{ width: '100%' }}
              >
                <i className="fas fa-sign-in-alt me-2"></i>
                Go to Login
              </button>
              
              <div className="mt-3">
                <small className="text-muted">
                  Redirecting to login page in 3 seconds...
                </small>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }

  // If user is authenticated, render the protected component
  return children;
};

export default ProtectedRoute;
