import React, { createContext, useContext, useState, useEffect } from 'react';
import { setCookie, getCookie, deleteCookie } from '../utils/cookieUtils';

const AuthContext = createContext();

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);

  // Check if user is logged in on app start
  useEffect(() => {
    const savedUser = localStorage.getItem('user');
    const accessToken = localStorage.getItem('access_token');
    const refreshToken = getCookie('refresh_token');
    
    if (savedUser && accessToken) {
      setUser(JSON.parse(savedUser));
    }
    setLoading(false);
  }, []);

  const login = async (email, password, rememberMe = false) => {
    try {
      const response = await fetch('https://localhost:7297/api/Auth/login', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          email: email,
          password: password,
          rememberMe: rememberMe
        })
      });

      const data = await response.json();

      if (response.ok) {
        // Login successful
        const userData = {
          id: data.user.id,
          email: data.user.email,
          userType: data.user.userType,
          loginTime: new Date().toISOString()
        };
        
        setUser(userData);
        localStorage.setItem('user', JSON.stringify(userData));
        
        // Store access_token in localStorage
        if (data.access_token) {
          localStorage.setItem('access_token', data.access_token);
        }
        
        // Store refresh_token in cookie
        if (data.refresh_token) {
          const cookieExpiryDays = rememberMe ? 30 : 1; // 30 days if remember me, 1 day otherwise
          setCookie('refresh_token', data.refresh_token, cookieExpiryDays);
        }
        
        return { success: true };
      } else {
        // Login failed
        return { success: false, error: data.message || 'Login failed. Please try again.' };
      }
    } catch (error) {
      console.error('Login error:', error);
      return { success: false, error: 'Network error. Please check your connection and try again.' };
    }
  };

  const register = async (email, password, userType) => {
    try {
      const response = await fetch('https://localhost:7297/api/Auth/register', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          email: email,
          password: password,
          userType: userType
        })
      });

      const data = await response.json();

      if (response.ok) {
        // Registration successful - don't auto login, just return success
        return { success: true };
      } else {
        // Registration failed
        return { success: false, error: data.message || 'Registration failed. Please try again.' };
      }
    } catch (error) {
      console.error('Registration error:', error);
      return { success: false, error: 'Network error. Please check your connection and try again.' };
    }
  };

  const logout = () => {
    setUser(null);
    localStorage.removeItem('user');
    localStorage.removeItem('access_token');
    deleteCookie('refresh_token');
  };

  const getAccessToken = () => {
    return localStorage.getItem('access_token');
  };

  const getRefreshToken = () => {
    return getCookie('refresh_token');
  };

  const value = {
    user,
    login,
    register,
    logout,
    getAccessToken,
    getRefreshToken,
    loading
  };

  return (
    <AuthContext.Provider value={value}>
      {!loading && children}
    </AuthContext.Provider>
  );
};
