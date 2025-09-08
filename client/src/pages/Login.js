import React, { useState, useEffect } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import Toast from '../components/Toast';
import VerifyCode from '../components/VerifyCode';

const Login = () => {
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    rememberMe: false
  });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [showToast, setShowToast] = useState(false);
  const [showVerifyCode, setShowVerifyCode] = useState(false);
  const [verificationData, setVerificationData] = useState(null);
  const [loginCredentials, setLoginCredentials] = useState(null);
  const [showVerifyToast, setShowVerifyToast] = useState(false);
  const [verifyToastMessage, setVerifyToastMessage] = useState('');
  const [verifyToastType, setVerifyToastType] = useState('error');
  
  const { login, verifyCode } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  useEffect(() => {
    document.title = 'Login - React App';
    
    // Check if user just registered
    if (location.search.includes('registered=true')) {
      setShowToast(true);
      // Clean up URL
      navigate('/login', { replace: true });
    }
  }, [location.search, navigate]);

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : value
    }));
    // Clear error when user starts typing
    if (error) setError('');
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError('');

    try {
      const result = await login(formData.email, formData.password, formData.rememberMe);
      
      if (result.success) {
        navigate('/home');
      } else if (result.requiresVerification) {
        // Show verification code input
        setVerificationData({
          email: result.email
        });
        // Store rememberMe in localStorage for later use
        localStorage.setItem('temp_rememberMe', JSON.stringify(formData.rememberMe));
        setLoginCredentials({
          email: formData.email
        });
        setShowVerifyCode(true);
        setError('');
      } else {
        setError(result.error);
      }
    } catch (err) {
      setError('An unexpected error occurred');
    } finally {
      setLoading(false);
    }
  };

  const handleVerifySuccess = async (data) => {
    if (loginCredentials) {
      try {
        // Get rememberMe from localStorage
        const rememberMe = JSON.parse(localStorage.getItem('temp_rememberMe') || 'false');
        
        const result = await verifyCode(
          loginCredentials.email, 
          data.code, 
          rememberMe
        );
        
        if (result.success) {
          // Clean up temporary data
          localStorage.removeItem('temp_rememberMe');
          setShowVerifyCode(false);
          setVerificationData(null);
          setLoginCredentials(null);
          navigate('/home');
        } else {
          // Show error in toast
          setVerifyToastMessage(result.error);
          setVerifyToastType('error');
          setShowVerifyToast(true);
        }
      } catch (err) {
        setVerifyToastMessage('Verification failed. Please try again.');
        setVerifyToastType('error');
        setShowVerifyToast(true);
      }
    }
  };

  const handleVerifyError = (error) => {
    setError(error);
  };

  const handleBackToLogin = () => {
    // Clean up temporary data
    localStorage.removeItem('temp_rememberMe');
    setShowVerifyCode(false);
    setVerificationData(null);
    setLoginCredentials(null);
    setError('');
  };

  return (
    <div className="container">
      <div className="d-flex justify-content-center align-items-center min-vh-100">
        <div className="card" style={{ width: '100%', maxWidth: '400px' }}>
          {showVerifyCode ? (
            <VerifyCode
              email={verificationData?.email}
              onVerifySuccess={handleVerifySuccess}
              onBack={handleBackToLogin}
              onError={handleVerifyError}
            />
          ) : (
            <>
              <div className="text-center mb-4">
                <h2>Login</h2>
                <p>Sign in to your account</p>
              </div>

              <form onSubmit={handleSubmit}>
                <div className="form-group">
                  <label htmlFor="email">Email</label>
                  <input
                    type="email"
                    id="email"
                    name="email"
                    value={formData.email}
                    onChange={handleChange}
                    required
                    placeholder="Enter your email"
                  />
                </div>

                <div className="form-group">
                  <label htmlFor="password">Password</label>
                  <input
                    type="password"
                    id="password"
                    name="password"
                    value={formData.password}
                    onChange={handleChange}
                    required
                    placeholder="Enter your password"
                  />
                </div>

                <div className="form-group">
                  <div style={{ display: 'flex', alignItems: 'center', marginBottom: '16px' }}>
                    <input
                      type="checkbox"
                      id="rememberMe"
                      name="rememberMe"
                      checked={formData.rememberMe}
                      onChange={handleChange}
                      style={{ 
                        marginRight: '8px', 
                        width: 'auto',
                        minWidth: '16px',
                        height: '16px',
                        flexShrink: 0
                      }}
                    />
                    <label htmlFor="rememberMe" style={{ 
                      margin: 0, 
                      fontSize: '14px', 
                      color: '#333',
                      whiteSpace: 'nowrap',
                      cursor: 'pointer'
                    }}>
                      Remember me
                    </label>
                  </div>
                </div>

                {error && <div className="error">{error}</div>}

                <button 
                  type="submit" 
                  className="btn btn-primary" 
                  style={{ width: '100%' }}
                  disabled={loading}
                >
                  {loading ? 'Logging in...' : 'Login'}
                </button>
              </form>

              <div className="text-center mt-4">
                <p>
                  <Link to="/forgot-password" style={{ color: '#007bff' }}>
                    Forgot your password?
                  </Link>
                </p>
                <p>
                  Don't have an account?{' '}
                  <Link to="/register" style={{ color: '#007bff' }}>
                    Register here
                  </Link>
                </p>
              </div>
            </>
          )}
        </div>
      </div>
      
      {showToast && (
        <Toast
          message="Registration successful!"
          type="success"
          duration={3000}
          onClose={() => setShowToast(false)}
        />
      )}
      
      {showVerifyToast && (
        <Toast
          message={verifyToastMessage}
          type={verifyToastType}
          duration={4000}
          onClose={() => setShowVerifyToast(false)}
        />
      )}
    </div>
  );
};

export default Login;
