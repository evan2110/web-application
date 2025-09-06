import React from 'react';

const FormControl = ({ 
  type = 'text', 
  value, 
  onChange, 
  placeholder, 
  className = '', 
  ...props 
}) => {
  return (
    <input
      type={type}
      value={value}
      onChange={onChange}
      placeholder={placeholder}
      className={`form-control ${className}`}
      {...props}
    />
  );
};

export default FormControl;
