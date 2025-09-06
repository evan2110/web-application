// JWT utility functions
export const decodeJWT = (token) => {
  try {
    if (!token) return null;
    
    const base64Url = token.split('.')[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split('')
        .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    );
    
    return JSON.parse(jsonPayload);
  } catch (error) {
    console.error('Error decoding JWT:', error);
    return null;
  }
};

export const isTokenExpired = (token) => {
  try {
    if (!token) return true;
    
    const decoded = decodeJWT(token);
    if (!decoded || !decoded.exp) return true;
    
    const currentTime = Math.floor(Date.now() / 1000);
    return decoded.exp < currentTime;
  } catch (error) {
    console.error('Error checking token expiry:', error);
    return true;
  }
};

export const getTokenExpiryTime = (token) => {
  try {
    if (!token) return null;
    
    const decoded = decodeJWT(token);
    if (!decoded || !decoded.exp) return null;
    
    return new Date(decoded.exp * 1000);
  } catch (error) {
    console.error('Error getting token expiry time:', error);
    return null;
  }
};

export const getTimeUntilExpiry = (token) => {
  try {
    if (!token) return 0;
    
    const decoded = decodeJWT(token);
    if (!decoded || !decoded.exp) return 0;
    
    const currentTime = Math.floor(Date.now() / 1000);
    return Math.max(0, decoded.exp - currentTime);
  } catch (error) {
    console.error('Error getting time until expiry:', error);
    return 0;
  }
};
