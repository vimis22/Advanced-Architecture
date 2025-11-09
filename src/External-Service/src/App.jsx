import { BrowserRouter, Routes, Route } from 'react-router-dom';
import HomePage from './pages/HomePage';
import CreateOrderPage from './pages/CreateOrderPage';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/create-order" element={<CreateOrderPage />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;