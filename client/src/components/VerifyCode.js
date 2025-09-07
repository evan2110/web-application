import React, { useState, useEffect } from 'react';
import Toast from './Toast';

const VerifyCode = ({ 
  email, 
  onVerifySuccess, 
  onBack, 
  onError 
}) => {
  const [code, setCode] = useState('');
  const [loading, setLoading] = useState(false);
  const [resendLoading, setResendLoading] = useState(false);
  const [timeLeft, setTimeLeft] = useState(60);
  const [canResend, setCanResend] = useState(false);
  const [error, setError] = useState('');
  const [showToast, setShowToast] = useState(false);
  const [toastMessage, setToastMessage] = useState('');
  const [toastType, setToastType] = useState('error');

  // Countdown timer
  useEffect(() => {
    if (timeLeft > 0) {
      const timer = setTimeout(() => {
        setTimeLeft(timeLeft - 1);
      }, 1000);
      return () => clearTimeout(timer);
    } else {
      setCanResend(true);
      // Show expired message when timer reaches 0
      setToastMessage('Verification code has expired. Please request a new one.');
      setToastType('warning');
      setShowToast(true);
    }
  }, [timeLeft]);

  const handleCodeChange = (e) => {
    const value = e.target.value.replace(/\D/g, ''); // Only allow numbers
    if (value.length <= 6) {
      setCode(value);
      setError('');
      // Hide toast when user starts typing
      if (showToast) {
        setShowToast(false);
      }
    }
  };

  const handleVerify = async (e) => {
    e.preventDefault();
    
    if (code.length !== 6) {
      setToastMessage('Please enter a 6-digit verification code');
      setToastType('error');
      setShowToast(true);
      return;
    }

    setLoading(true);
    setError('');

    try {
      // Call the parent's verify success callback with the code
      onVerifySuccess({ code: code });
    } catch (error) {
      console.error('Verification error:', error);
      setToastMessage('Verification failed. Please try again.');
      setToastType('error');
      setShowToast(true);
    } finally {
      setLoading(false);
    }
  };

  const handleResendCode = async () => {
    setResendLoading(true);
    setError('');

    try {
      const response = await fetch(`https://localhost:7297/api/Auth/sendMail?email=${encodeURIComponent(email)}`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        }
      });

      let data;
      const contentType = response.headers.get('content-type');
      
      if (contentType && contentType.includes('application/json')) {
        try {
          data = await response.json();
        } catch (jsonError) {
          console.error('JSON parse error:', jsonError);
          data = { message: 'Invalid response format' };
        }
      } else {
        // If response is not JSON, treat as plain text
        const textResponse = await response.text();
        data = { message: textResponse };
      }

      if (response.ok) {
        setTimeLeft(60);
        setCanResend(false);
        setCode('');
        // Show success message
        setToastMessage(data.message || 'A new verification code has been sent to your email!');
        setToastType('success');
        setShowToast(true);
        setError('');
      } else {
        setToastMessage(data.message || 'Failed to resend verification code. Please try again.');
        setToastType('error');
        setShowToast(true);
      }
    } catch (error) {
      console.error('Resend error:', error);
      setToastMessage('Network error. Please check your connection and try again.');
      setToastType('error');
      setShowToast(true);
    } finally {
      setResendLoading(false);
    }
  };

  const formatTime = (seconds) => {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  return (
    <div className="verify-code-container">
      <div className="text-center mb-4">
        <h3>Email Verification</h3>
        <p className="text-muted">
          We've sent a 6-digit verification code to<br />
          <strong>{email}</strong>
        </p>
      </div>

      <form onSubmit={handleVerify}>
        <div className="form-group">
          <label htmlFor="verificationCode">Verification Code</label>
          <input
            type="text"
            id="verificationCode"
            value={code}
            onChange={handleCodeChange}
            placeholder={timeLeft === 0 ? "Code expired - please resend" : "Enter 6-digit code"}
            maxLength="6"
            className="form-control text-center"
            style={{ 
              fontSize: '1.5rem', 
              letterSpacing: '0.5rem',
              fontFamily: 'monospace',
              opacity: timeLeft === 0 ? 0.6 : 1,
              cursor: timeLeft === 0 ? 'not-allowed' : 'text'
            }}
            autoComplete="off"
            autoFocus
            disabled={timeLeft === 0}
          />
        </div>


        <button 
          type="submit" 
          className="btn btn-primary" 
          style={{ width: '100%', marginBottom: '16px' }}
          disabled={loading || code.length !== 6 || timeLeft === 0}
        >
          {loading ? 'Verifying...' : timeLeft === 0 ? 'Code Expired' : 'Verify Code'}
        </button>

        <div className="text-center">
          {canResend ? (
            <button
              type="button"
              className="btn btn-link"
              onClick={handleResendCode}
              disabled={resendLoading}
              style={{ color: '#007bff', textDecoration: 'none' }}
            >
              {resendLoading ? 'Sending...' : 'Resend Code'}
            </button>
          ) : (
            <p className="text-muted">
              Resend code in {formatTime(timeLeft)}
            </p>
          )}
        </div>

        <div className="text-center mt-3">
          <button
            type="button"
            className="btn btn-link"
            onClick={onBack}
            style={{ color: '#6c757d', textDecoration: 'none' }}
          >
            ← Back to Login
          </button>
        </div>
      </form>
      
      {showToast && (
        <Toast
          message={toastMessage}
          type={toastType}
          duration={4000}
          onClose={() => setShowToast(false)}
        />
      )}
    </div>
  );
};

export default VerifyCode;
