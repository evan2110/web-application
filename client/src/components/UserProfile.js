import React, { useState, useEffect } from 'react';

const UserProfile = ({ user }) => {
  const [profileData, setProfileData] = useState({
    name: user?.name || '',
    email: user?.email || '',
    userType: user?.userType || '',
    bio: '',
    location: '',
    website: ''
  });
  const [isEditing, setIsEditing] = useState(false);
  const [message, setMessage] = useState('');

  useEffect(() => {
    // Update profile data when user changes (from token)
    if (user) {
      setProfileData(prev => ({
        ...prev,
        email: user.email || prev.email,
        userType: user.userType || prev.userType,
        name: user.name || prev.name
      }));
    }
    
    // Load additional profile data from localStorage
    const savedProfile = localStorage.getItem('userProfile');
    if (savedProfile) {
      const parsedProfile = JSON.parse(savedProfile);
      setProfileData(prev => ({ ...prev, ...parsedProfile }));
    }
  }, [user]);

  const handleChange = (e) => {
    const { name, value } = e.target;
    setProfileData(prev => ({
      ...prev,
      [name]: value
    }));
  };

  const handleSave = () => {
    // Save to localStorage (in real app, this would be API call)
    localStorage.setItem('userProfile', JSON.stringify(profileData));
    setIsEditing(false);
    setMessage('Profile updated successfully!');
    setTimeout(() => setMessage(''), 3000);
  };

  const handleCancel = () => {
    // Reset to original data
    setProfileData({
      name: user?.name || '',
      email: user?.email || '',
      userType: user?.userType || '',
      bio: profileData.bio,
      location: profileData.location,
      website: profileData.website
    });
    setIsEditing(false);
  };

  return (
    <div className="card">

      {message && (
        <div className="success mb-3">{message}</div>
      )}

      <div className="profile-content">
        <div className="form-group">
          <label>Email</label>
          {isEditing ? (
            <input
              type="email"
              name="email"
              value={profileData.email}
              onChange={handleChange}
              className="form-control"
            />
          ) : (
            <p>{profileData.email}</p>
          )}
        </div>

        <div className="form-group">
          <label>User Type</label>
          {isEditing ? (
            <select
              name="userType"
              value={profileData.userType}
              onChange={handleChange}
              className="form-control"
            >
              <option value="user">User</option>
              <option value="admin">Admin</option>
            </select>
          ) : (
            <p>
              <span className={`badge ${profileData.userType === 'admin' ? 'bg-danger' : 'bg-primary'}`}>
                {profileData.userType || 'User'}
              </span>
            </p>
          )}
        </div>

        {isEditing && (
          <div className="d-flex gap-2">
            <button className="btn btn-primary" onClick={handleSave}>
              Save Changes
            </button>
            <button className="btn btn-secondary" onClick={handleCancel}>
              Cancel
            </button>
          </div>
        )}
      </div>
    </div>
  );
};

export default UserProfile;
