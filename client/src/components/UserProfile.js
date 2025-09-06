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
    // Load additional profile data from localStorage
    const savedProfile = localStorage.getItem('userProfile');
    if (savedProfile) {
      const parsedProfile = JSON.parse(savedProfile);
      setProfileData(prev => ({ ...prev, ...parsedProfile }));
    }
  }, []);

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
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h3>User Profile</h3>
        <button 
          className="btn btn-primary"
          onClick={() => setIsEditing(!isEditing)}
        >
          {isEditing ? 'Cancel' : 'Edit Profile'}
        </button>
      </div>

      {message && (
        <div className="success mb-3">{message}</div>
      )}

      <div className="profile-content">
        <div className="form-group">
          <label>Name</label>
          {isEditing ? (
            <input
              type="text"
              name="name"
              value={profileData.name}
              onChange={handleChange}
              className="form-control"
            />
          ) : (
            <p>{profileData.name}</p>
          )}
        </div>

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

        <div className="form-group">
          <label>Bio</label>
          {isEditing ? (
            <textarea
              name="bio"
              value={profileData.bio}
              onChange={handleChange}
              className="form-control"
              rows="3"
              placeholder="Tell us about yourself..."
            />
          ) : (
            <p>{profileData.bio || 'No bio provided'}</p>
          )}
        </div>

        <div className="form-group">
          <label>Location</label>
          {isEditing ? (
            <input
              type="text"
              name="location"
              value={profileData.location}
              onChange={handleChange}
              className="form-control"
              placeholder="Your location"
            />
          ) : (
            <p>{profileData.location || 'No location provided'}</p>
          )}
        </div>

        <div className="form-group">
          <label>Website</label>
          {isEditing ? (
            <input
              type="url"
              name="website"
              value={profileData.website}
              onChange={handleChange}
              className="form-control"
              placeholder="https://yourwebsite.com"
            />
          ) : (
            <p>
              {profileData.website ? (
                <a href={profileData.website} target="_blank" rel="noopener noreferrer">
                  {profileData.website}
                </a>
              ) : (
                'No website provided'
              )}
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
