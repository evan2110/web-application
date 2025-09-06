import React, { useState, useEffect, useCallback } from 'react';

const TodoList = () => {
  const [todos, setTodos] = useState([]);
  const [newTodo, setNewTodo] = useState('');
  const [filter, setFilter] = useState('all'); // all, active, completed

  // Load todos from localStorage on component mount
  useEffect(() => {
    const savedTodos = localStorage.getItem('todos');
    if (savedTodos) {
      setTodos(JSON.parse(savedTodos));
    }
  }, []);

  // Save todos to localStorage whenever todos change
  useEffect(() => {
    localStorage.setItem('todos', JSON.stringify(todos));
  }, [todos]);

  const addTodo = useCallback(() => {
    if (newTodo.trim()) {
      const todo = {
        id: Date.now(),
        text: newTodo.trim(),
        completed: false,
        createdAt: new Date().toISOString()
      };
      setTodos(prev => [...prev, todo]);
      setNewTodo('');
    }
  }, [newTodo]);

  const toggleTodo = useCallback((id) => {
    setTodos(prev => 
      prev.map(todo => 
        todo.id === id ? { ...todo, completed: !todo.completed } : todo
      )
    );
  }, []);

  const deleteTodo = useCallback((id) => {
    setTodos(prev => prev.filter(todo => todo.id !== id));
  }, []);

  const clearCompleted = useCallback(() => {
    setTodos(prev => prev.filter(todo => !todo.completed));
  }, []);

  const filteredTodos = todos.filter(todo => {
    if (filter === 'active') return !todo.completed;
    if (filter === 'completed') return todo.completed;
    return true;
  });

  const completedCount = todos.filter(todo => todo.completed).length;
  const activeCount = todos.length - completedCount;

  return (
    <div className="card">
      <h3 className="mb-4">Todo List</h3>
      
      {/* Add Todo Form */}
      <div className="d-flex gap-2 mb-4">
        <input
          type="text"
          value={newTodo}
          onChange={(e) => setNewTodo(e.target.value)}
          onKeyPress={(e) => e.key === 'Enter' && addTodo()}
          placeholder="Add a new todo..."
          className="form-control"
          style={{ flex: 1 }}
        />
        <button 
          className="btn btn-primary" 
          onClick={addTodo}
          disabled={!newTodo.trim()}
        >
          Add
        </button>
      </div>

      {/* Filter Buttons */}
      <div className="d-flex gap-2 mb-4">
        <button 
          className={`btn ${filter === 'all' ? 'btn-primary' : 'btn-secondary'}`}
          onClick={() => setFilter('all')}
        >
          All ({todos.length})
        </button>
        <button 
          className={`btn ${filter === 'active' ? 'btn-primary' : 'btn-secondary'}`}
          onClick={() => setFilter('active')}
        >
          Active ({activeCount})
        </button>
        <button 
          className={`btn ${filter === 'completed' ? 'btn-primary' : 'btn-secondary'}`}
          onClick={() => setFilter('completed')}
        >
          Completed ({completedCount})
        </button>
      </div>

      {/* Todo List */}
      <div className="todo-list">
        {filteredTodos.length === 0 ? (
          <p className="text-center text-muted">
            {filter === 'all' ? 'No todos yet. Add one above!' : 
             filter === 'active' ? 'No active todos!' : 
             'No completed todos!'}
          </p>
        ) : (
          filteredTodos.map(todo => (
            <div 
              key={todo.id} 
              className={`todo-item ${todo.completed ? 'completed' : ''}`}
              style={{
                display: 'flex',
                alignItems: 'center',
                padding: '12px',
                border: '1px solid #ddd',
                borderRadius: '6px',
                marginBottom: '8px',
                backgroundColor: todo.completed ? '#f8f9fa' : 'white'
              }}
            >
              <input
                type="checkbox"
                checked={todo.completed}
                onChange={() => toggleTodo(todo.id)}
                style={{ marginRight: '12px' }}
              />
              <span 
                style={{ 
                  flex: 1, 
                  textDecoration: todo.completed ? 'line-through' : 'none',
                  color: todo.completed ? '#6c757d' : 'inherit'
                }}
              >
                {todo.text}
              </span>
              <button 
                className="btn btn-secondary"
                onClick={() => deleteTodo(todo.id)}
                style={{ padding: '4px 8px', fontSize: '12px' }}
              >
                Delete
              </button>
            </div>
          ))
        )}
      </div>

      {/* Clear Completed Button */}
      {completedCount > 0 && (
        <div className="text-center mt-4">
          <button 
            className="btn btn-secondary"
            onClick={clearCompleted}
          >
            Clear Completed ({completedCount})
          </button>
        </div>
      )}
    </div>
  );
};

export default TodoList;
