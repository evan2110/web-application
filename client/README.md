# React App - Complete ReactJS Application

This is a comprehensive ReactJS application showcasing various React concepts including components, hooks, context, routing, and more.

## Features

### Pages
- **Home Page**: Dashboard with multiple components and features
- **Login Page**: User authentication with form validation
- **Register Page**: User registration with form validation

### Components
- **Navbar**: Navigation component with authentication state
- **UserProfile**: User profile management with edit functionality
- **TodoList**: Full-featured todo list with filtering and persistence
- **Counter**: Interactive counter with history tracking
- **FormControl**: Reusable form input component

### Hooks
- **useAuth**: Authentication context hook
- **useLocalStorage**: Custom hook for localStorage management
- **useDebounce**: Custom hook for debouncing values

### Context
- **AuthContext**: Global authentication state management

### Utilities
- **helpers.js**: Utility functions for validation, formatting, etc.

## Technologies Used

- React 18.2.0
- React Router DOM 6.3.0
- React Hooks (useState, useEffect, useContext, useCallback, useMemo)
- Context API
- Local Storage
- CSS3 with modern styling

## Getting Started

### Prerequisites
- Node.js (version 14 or higher)
- npm or yarn

### Installation

1. Clone the repository
2. Install dependencies:
   ```bash
   npm install
   ```

3. Start the development server:
   ```bash
   npm start
   ```

4. Open [http://localhost:3000](http://localhost:3000) to view it in the browser.

### Demo Credentials
- Email: admin@example.com
- Password: password

## Project Structure

```
src/
├── components/          # Reusable components
│   ├── Navbar.js
│   ├── UserProfile.js
│   ├── TodoList.js
│   ├── Counter.js
│   └── FormControl.js
├── contexts/           # React Context
│   └── AuthContext.js
├── hooks/              # Custom hooks
│   ├── useLocalStorage.js
│   └── useDebounce.js
├── pages/              # Page components
│   ├── Home.js
│   ├── Login.js
│   └── Register.js
├── utils/              # Utility functions
│   └── helpers.js
├── App.js              # Main app component
├── App.css             # App styles
├── index.js            # Entry point
└── index.css           # Global styles
```

## Features Explained

### Authentication
- Context-based authentication state management
- Protected and public routes
- Form validation for login and registration
- Persistent login state using localStorage

### Todo List
- Add, edit, delete todos
- Filter by status (all, active, completed)
- Persistent storage
- Real-time updates

### Counter
- Increment/decrement with custom step
- History tracking
- Quick set buttons
- Number analysis (even/odd, prime)
- Persistent state

### User Profile
- Editable profile information
- Form validation
- Persistent storage
- Real-time updates

## Available Scripts

- `npm start`: Runs the app in development mode
- `npm build`: Builds the app for production
- `npm test`: Launches the test runner
- `npm eject`: Ejects from Create React App (one-way operation)

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test your changes
5. Submit a pull request

## License

This project is open source and available under the [MIT License](LICENSE).
