// Utility functions for the React app

export const formatDate = (dateString) => {
  const date = new Date(dateString);
  return date.toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  });
};

export const validateEmail = (email) => {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  return emailRegex.test(email);
};

export const validatePassword = (password) => {
  const minLength = 6;
  const hasUpperCase = /[A-Z]/.test(password);
  const hasLowerCase = /[a-z]/.test(password);
  const hasNumbers = /\d/.test(password);
  
  return {
    isValid: password.length >= minLength,
    minLength,
    hasUpperCase,
    hasLowerCase,
    hasNumbers,
    strength: password.length >= 8 && hasUpperCase && hasLowerCase && hasNumbers ? 'strong' :
              password.length >= 6 ? 'medium' : 'weak'
  };
};

export const generateId = () => {
  return Date.now().toString(36) + Math.random().toString(36).substr(2);
};

export const capitalizeFirst = (str) => {
  return str.charAt(0).toUpperCase() + str.slice(1);
};

export const truncateText = (text, maxLength) => {
  if (text.length <= maxLength) return text;
  return text.substr(0, maxLength) + '...';
};

export const debounce = (func, wait) => {
  let timeout;
  return function executedFunction(...args) {
    const later = () => {
      clearTimeout(timeout);
      func(...args);
    };
    clearTimeout(timeout);
    timeout = setTimeout(later, wait);
  };
};

// API call helper with blacklist error handling
export const apiCall = async (url, options = {}) => {
  try {
    const response = await fetch(url, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        ...options.headers,
      },
    });

    // Check if response indicates token is blacklisted
    if (response.status === 401) {
      const data = await response.json().catch(() => ({}));
      if (data.message && data.message.includes('blacklisted')) {
        // Token is blacklisted, clear local storage and redirect to login
        localStorage.removeItem('access_token');
        localStorage.removeItem('user');
        window.location.href = '/login';
        throw new Error('Token has been blacklisted. Please login again.');
      }
    }

    return response;
  } catch (error) {
    console.error('API call error:', error);
    throw error;
  }
};

// Helper to check if error is related to blacklisted token
export const isBlacklistError = (error) => {
  return error.message && error.message.includes('blacklisted');
};