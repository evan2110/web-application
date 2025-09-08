import React, { useEffect, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';

const VerifyEmail = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const [status, setStatus] = useState('pending'); // 'pending' | 'success' | 'error'
  const [message, setMessage] = useState('');

  useEffect(() => {
    const params = new URLSearchParams(location.search);
    const token = params.get('token');
    if (!token) {
      setStatus('error');
      setMessage('Invalid verification link.');
      return;
    }

    const verify = async () => {
      try {
        const base = process.env.REACT_APP_API_BASE_URL || '';
        const url = `${base}/api/Auth/verify-email?token=${encodeURIComponent(token)}`.replace('//api', '/api');
        const res = await fetch(url, { credentials: 'include' });
        if (res.ok) {
          setStatus('success');
          setMessage('Your email has been verified successfully.');
        } else {
          const data = await res.json().catch(() => ({}));
          setStatus('error');
          setMessage(data?.message || 'Verification failed.');
        }
      } catch (e) {
        setStatus('error');
        setMessage('Verification failed due to a network error.');
      }
    };

    verify();
  }, [location.search]);

  return (
    <div className="container" style={{ maxWidth: 480, margin: '40px auto', textAlign: 'center' }}>
      <h2>Email Verification</h2>
      {status === 'pending' && <p>Verifying your email, please wait...</p>}
      {status === 'success' && (
        <>
          <p style={{ color: 'green' }}>{message}</p>
          <button onClick={() => navigate('/login')} className="btn">Go to Login</button>
        </>
      )}
      {status === 'error' && (
        <>
          <p style={{ color: 'crimson' }}>{message}</p>
          <button onClick={() => navigate('/login')} className="btn">Go to Login</button>
        </>
      )}
    </div>
  );
};

export default VerifyEmail;


