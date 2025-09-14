import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import Landing from './pages/Landing';
import Privacy from './pages/Privacy';
import Test from './pages/Test';
import End from './pages/End';

function App() {
  return (
    <Router>
      <div className="App">
        <Routes>
          <Route path="/" element={<Landing />} />
          <Route path="/privacy" element={<Privacy />} />
          <Route path="/test" element={<Test />} />
          <Route path="/end" element={<End />} />
        </Routes>
      </div>
    </Router>
  );
}

export default App;
