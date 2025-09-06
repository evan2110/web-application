import React, { useState, useEffect, useCallback, useMemo } from 'react';

const Counter = () => {
  const [count, setCount] = useState(0);
  const [step, setStep] = useState(1);
  const [history, setHistory] = useState([]);

  // Load counter data from localStorage
  useEffect(() => {
    const savedData = localStorage.getItem('counterData');
    if (savedData) {
      const { count: savedCount, step: savedStep, history: savedHistory } = JSON.parse(savedData);
      setCount(savedCount);
      setStep(savedStep);
      setHistory(savedHistory || []);
    }
  }, []);

  // Save counter data to localStorage
  useEffect(() => {
    const counterData = { count, step, history };
    localStorage.setItem('counterData', JSON.stringify(counterData));
  }, [count, step, history]);

  const increment = useCallback(() => {
    setCount(prev => prev + step);
    setHistory(prev => [...prev, { action: 'increment', value: step, timestamp: new Date().toISOString() }]);
  }, [step]);

  const decrement = useCallback(() => {
    setCount(prev => prev - step);
    setHistory(prev => [...prev, { action: 'decrement', value: step, timestamp: new Date().toISOString() }]);
  }, [step]);

  const reset = useCallback(() => {
    setCount(0);
    setHistory(prev => [...prev, { action: 'reset', value: 0, timestamp: new Date().toISOString() }]);
  }, []);

  const setCustomValue = useCallback((value) => {
    setCount(value);
    setHistory(prev => [...prev, { action: 'set', value, timestamp: new Date().toISOString() }]);
  }, []);

  // Memoized calculations
  const isEven = useMemo(() => count % 2 === 0, [count]);
  const isPrime = useMemo(() => {
    if (count < 2) return false;
    for (let i = 2; i <= Math.sqrt(count); i++) {
      if (count % i === 0) return false;
    }
    return true;
  }, [count]);

  const recentHistory = useMemo(() => history.slice(-5), [history]);

  return (
    <div className="card">
      <h3 className="mb-4">Counter with History</h3>
      
      {/* Counter Display */}
      <div className="text-center mb-4">
        <div 
          style={{
            fontSize: '3rem',
            fontWeight: 'bold',
            color: isPrime ? '#28a745' : isEven ? '#007bff' : '#dc3545',
            marginBottom: '1rem'
          }}
        >
          {count}
        </div>
        
        <div className="mb-3">
          <span className={`badge ${isEven ? 'badge-primary' : 'badge-secondary'}`}>
            {isEven ? 'Even' : 'Odd'}
          </span>
          {isPrime && (
            <span className="badge badge-success ml-2">Prime</span>
          )}
        </div>
      </div>

      {/* Step Control */}
      <div className="form-group mb-4">
        <label>Step Size</label>
        <input
          type="number"
          value={step}
          onChange={(e) => setStep(Number(e.target.value))}
          min="1"
          max="100"
          className="form-control"
        />
      </div>

      {/* Counter Controls */}
      <div className="d-flex justify-content-center gap-2 mb-4">
        <button className="btn btn-primary" onClick={decrement}>
          -{step}
        </button>
        <button className="btn btn-secondary" onClick={reset}>
          Reset
        </button>
        <button className="btn btn-primary" onClick={increment}>
          +{step}
        </button>
      </div>

      {/* Quick Set Buttons */}
      <div className="text-center mb-4">
        <p>Quick Set:</p>
        <div className="d-flex justify-content-center gap-2 flex-wrap">
          {[0, 10, 25, 50, 100].map(value => (
            <button
              key={value}
              className="btn btn-outline-primary"
              onClick={() => setCustomValue(value)}
              style={{ fontSize: '0.9rem', padding: '4px 8px' }}
            >
              {value}
            </button>
          ))}
        </div>
      </div>

      {/* History */}
      <div className="history-section">
        <h5>Recent History</h5>
        {recentHistory.length === 0 ? (
          <p className="text-muted">No actions yet</p>
        ) : (
          <div className="history-list">
            {recentHistory.slice().reverse().map((entry, index) => (
              <div 
                key={index}
                className="history-item"
                style={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                  padding: '8px 12px',
                  backgroundColor: '#f8f9fa',
                  borderRadius: '4px',
                  marginBottom: '4px',
                  fontSize: '0.9rem'
                }}
              >
                <span>
                  {entry.action === 'increment' && `+${entry.value}`}
                  {entry.action === 'decrement' && `-${entry.value}`}
                  {entry.action === 'reset' && 'Reset to 0'}
                  {entry.action === 'set' && `Set to ${entry.value}`}
                </span>
                <span className="text-muted">
                  {new Date(entry.timestamp).toLocaleTimeString()}
                </span>
              </div>
            ))}
          </div>
        )}
      </div>

      <style jsx>{`
        .badge {
          padding: 4px 8px;
          border-radius: 4px;
          font-size: 0.8rem;
          font-weight: 500;
        }
        .badge-primary {
          background-color: #007bff;
          color: white;
        }
        .badge-secondary {
          background-color: #6c757d;
          color: white;
        }
        .badge-success {
          background-color: #28a745;
          color: white;
        }
        .ml-2 {
          margin-left: 8px;
        }
      `}</style>
    </div>
  );
};

export default Counter;
