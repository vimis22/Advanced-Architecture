import { BrowserRouter, Routes, Route } from 'react-router-dom';
import HomeScreen from "./components/screens/newly/HomeScreen";
import OrderScreen from "./components/screens/newly/OrderScreen";

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<HomeScreen />} />
        <Route path="/create-order" element={<OrderScreen />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
