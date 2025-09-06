import React, { createContext, useContext, useState, useEffect } from 'react';
import { setCookie, getCookie, deleteCookie } from '../utils/cookieUtils';
import { isTokenExpired, getTimeUntilExpiry } from '../utils/jwtUtils';

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
  const [tokenRefreshing, setTokenRefreshing] = useState(false);

  // Check if user is logged in on app start
  useEffect(() => {
    const initializeAuth = async () => {
      const savedUser = localStorage.getItem('user');
      const accessToken = localStorage.getItem('access_token');
      const refreshToken = getCookie('refresh_token');
      
      if (savedUser && accessToken && refreshToken) {
        // Check if access token is expired
        if (isTokenExpired(accessToken)) {
          console.log('Access token expired, attempting refresh...');
          const refreshResult = await refreshAccessToken();
          
          if (refreshResult.success) {
            setUser(JSON.parse(savedUser));
          } else {
            // Refresh failed, clear everything
            await logout();
          }
        } else {
          // Access token is still valid
          setUser(JSON.parse(savedUser));
        }
      }
      setLoading(false);
    };

    initializeAuth();
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

  const logout = async () => {
    try {
      const refreshToken = getCookie('refresh_token');
      
      // Call logout API if refresh token exists
      if (refreshToken) {
        try {
          await fetch('https://localhost:7297/api/Auth/logout', {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
            },
            body: JSON.stringify({
              refreshToken: refreshToken
            })
          });
        } catch (error) {
          console.error('Logout API error:', error);
          // Continue with local logout even if API fails
        }
      }
    } catch (error) {
      console.error('Logout error:', error);
    } finally {
      // Always clear local data
      setUser(null);
      localStorage.removeItem('user');
      localStorage.removeItem('access_token');
      deleteCookie('refresh_token');
    }
  };

  const getAccessToken = () => {
    return localStorage.getItem('access_token');
  };

  const getRefreshToken = () => {
    return getCookie('refresh_token');
  };

  const refreshAccessToken = async () => {
    try {
      setTokenRefreshing(true);
      const refreshToken = getCookie('refresh_token');
      
      if (!refreshToken) {
        throw new Error('No refresh token available');
      }

      const response = await fetch('https://localhost:7297/api/Auth/refresh', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          refreshToken: refreshToken
        })
      });

      const data = await response.json();

      if (response.ok) {
        // Update access token
        if (data.access_token) {
          localStorage.setItem('access_token', data.access_token);
        }
        
        // Update refresh token if provided
        if (data.refresh_token) {
          setCookie('refresh_token', data.refresh_token, 30); // 30 days
        }
        
        return { success: true, accessToken: data.access_token };
      } else {
        // Refresh failed, clear tokens and logout
        await logout();
        return { success: false, error: data.message || 'Token refresh failed' };
      }
    } catch (error) {
      console.error('Token refresh error:', error);
      await logout();
      return { success: false, error: 'Token refresh failed' };
    } finally {
      setTokenRefreshing(false);
    }
  };

  const checkAndRefreshToken = async () => {
    const accessToken = localStorage.getItem('access_token');
    const refreshToken = getCookie('refresh_token');
    const savedUser = localStorage.getItem('user');
    
    // If no tokens, user is not logged in
    if (!accessToken || !refreshToken) {
      return false;
    }
    
    // If access token is not expired, user is authenticated
    if (!isTokenExpired(accessToken)) {
      // Make sure user state is set if not already
      if (!user && savedUser) {
        setUser(JSON.parse(savedUser));
      }
      return true;
    }
    
    // Access token is expired, try to refresh
    console.log('Access token expired, attempting refresh...');
    const refreshResult = await refreshAccessToken();
    
    if (refreshResult.success && savedUser) {
      // Set user state after successful refresh
      setUser(JSON.parse(savedUser));
    }
    
    return refreshResult.success;
  };

  const value = {
    user,
    login,
    register,
    logout,
    getAccessToken,
    getRefreshToken,
    refreshAccessToken,
    checkAndRefreshToken,
    loading,
    tokenRefreshing
  };

  return (
    <AuthContext.Provider value={value}>
      {!loading && children}
    </AuthContext.Provider>
  );
};
