import React, { createContext, useContext, useState, useEffect } from 'react';
import { setCookie, getCookie, deleteCookie } from '../utils/cookieUtils';
import { isTokenExpired, getTimeUntilExpiry, decodeJWT } from '../utils/jwtUtils';

const AuthContext = createContext();

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};

// Function to get user info from access token
const getUserFromToken = (accessToken) => {
  try {
    if (!accessToken) return null;
    
    const decoded = decodeJWT(accessToken);
    if (!decoded) return null;
    
    // Extract user information from token claims using the correct Microsoft claims format
    return {
      id: decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] || decoded.sub || decoded.userId || decoded.id,
      email: decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] || decoded.email || decoded.email_address,
      userType: decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || decoded.userType || decoded.role || decoded.user_type || 'User',
      loginTime: new Date().toISOString()
    };
  } catch (error) {
    console.error('Error getting user from token:', error);
    return null;
  }
};

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);
  const [tokenRefreshing, setTokenRefreshing] = useState(false);

  // Check if user is logged in on app start
  useEffect(() => {
    const initializeAuth = async () => {
      const accessToken = localStorage.getItem('access_token');
      const refreshToken = getCookie('refresh_token');
      
      if (accessToken && refreshToken) {
        // Check if access token is expired
        if (isTokenExpired(accessToken)) {
          console.log('Access token expired, attempting refresh...');
          const refreshResult = await refreshAccessToken();
          
          if (refreshResult.success) {
            // Get user info from refreshed token
            const userFromToken = getUserFromToken(refreshResult.accessToken);
            if (userFromToken) {
              setUser(userFromToken);
              localStorage.setItem('user', JSON.stringify(userFromToken));
            }
          } else {
            // Refresh failed, clear everything
            await logout();
          }
        } else {
          // Access token is still valid, get user info from token
          const userFromToken = getUserFromToken(accessToken);
          if (userFromToken) {
            setUser(userFromToken);
            localStorage.setItem('user', JSON.stringify(userFromToken));
          } else {
            // Fallback to saved user data if token decode fails
            const savedUser = localStorage.getItem('user');
            if (savedUser) {
              setUser(JSON.parse(savedUser));
            }
          }
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
        // Login successful - get user info from access token
        const userFromToken = getUserFromToken(data.access_token);
        const userData = userFromToken || {
          id: data.user?.id,
          email: data.user?.email,
          userType: data.user?.userType || 'User',
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
      } else if (response.status === 409) {
        // Requires verification - return user info for verification
        return { 
          success: false, 
          requiresVerification: true, 
          email: email,
          message: data.message || 'Please verify your email to complete login.'
        };
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
      const accessToken = localStorage.getItem('access_token');
      const refreshToken = getCookie('refresh_token');
      
      // Call logout API if refresh token exists
      if (refreshToken) {
        try {
          const response = await fetch('https://localhost:7297/api/Auth/logout', {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
            },
            body: JSON.stringify({
              accessToken: accessToken,
              refreshToken: refreshToken
            })
          });

          if (response.ok) {
            console.log('Logout successful - access token blacklisted');
          } else {
            console.warn('Logout API returned error:', response.status);
          }
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
        console.warn('Token refresh failed:', data.message);
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

  const verifyCode = async (email, code, rememberMe = false) => {
    try {
      console.log('Verifying code...');
      console.log('Email:', email);
      console.log('Code:', code);
      console.log('RememberMe:', rememberMe);
      
      const requestBody = {
        email: email,
        userCodeVerify: code,
        rememberMe: rememberMe
      };
      
      console.log('Request body:', requestBody);
      
      const verifyResponse = await fetch('https://localhost:7297/api/Auth/verify', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(requestBody)
      });

      const verifyData = await verifyResponse.json();
      
      console.log('Response status:', verifyResponse.status);
      console.log('Response data:', verifyData);

      if (verifyResponse.ok) {
        // Verification successful - get user info from access token
        const userFromToken = getUserFromToken(verifyData.access_token);
        const userData = userFromToken || {
          id: verifyData.user?.id,
          email: verifyData.user?.email,
          userType: verifyData.user?.userType || 'User',
          loginTime: new Date().toISOString()
        };
        
        setUser(userData);
        localStorage.setItem('user', JSON.stringify(userData));
        
        // Store access_token in localStorage
        if (verifyData.access_token) {
          localStorage.setItem('access_token', verifyData.access_token);
        }
        
        // Store refresh_token in cookie
        if (verifyData.refresh_token) {
          const cookieExpiryDays = rememberMe ? 30 : 1;
          setCookie('refresh_token', verifyData.refresh_token, cookieExpiryDays);
        }
        
        return { success: true };
      } else {
        return { success: false, error: verifyData.message || 'Verification failed. Please try again.' };
      }
    } catch (error) {
      console.error('Verification error:', error);
      return { success: false, error: 'Network error. Please check your connection and try again.' };
    }
  };

  const checkAndRefreshToken = async () => {
    const accessToken = localStorage.getItem('access_token');
    const refreshToken = getCookie('refresh_token');
    
    // If no tokens, user is not logged in
    if (!accessToken || !refreshToken) {
      return false;
    }
    
    // If access token is not expired, user is authenticated
    if (!isTokenExpired(accessToken)) {
      // Get user info from token and update state if needed
      const userFromToken = getUserFromToken(accessToken);
      if (userFromToken && (!user || user.id !== userFromToken.id)) {
        setUser(userFromToken);
        localStorage.setItem('user', JSON.stringify(userFromToken));
      }
      return true;
    }
    
    // Access token is expired, try to refresh
    console.log('Access token expired, attempting refresh...');
    const refreshResult = await refreshAccessToken();
    
    if (refreshResult.success) {
      // Get user info from refreshed token
      const userFromToken = getUserFromToken(refreshResult.accessToken);
      if (userFromToken) {
        setUser(userFromToken);
        localStorage.setItem('user', JSON.stringify(userFromToken));
      }
    }
    
    return refreshResult.success;
  };


  const value = {
    user,
    login,
    register,
    logout,
    verifyCode,
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
