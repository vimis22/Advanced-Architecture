import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import axios from 'axios';
import './CreateOrderPage.css';

function CreateOrderPage() {
  const navigate = useNavigate();
  const [formData, setFormData] = useState({
    title: '',
    author: '',
    pages: '',
    coverType: 'HARDCOVER',
    quantity: ''
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const handleChange = (e) => {
    setFormData({
      ...formData,
      [e.target.name]: e.target.value
    });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setSuccess('');
    setLoading(true);

    // Prepare data (convert pages and quantity to numbers)
    const orderData = {
      title: formData.title,
      author: formData.author,
      pages: parseInt(formData.pages),
      coverType: formData.coverType,
      quantity: parseInt(formData.quantity)
    };

    try {
      const response = await axios.post(
        'http://localhost:8080/api/v1/orchestrator/orders',
        orderData,
        {
          headers: {
            'Content-Type': 'application/json',
            'Accept': 'application/json'
          }
        }
      );

      setSuccess(`Order created successfully! Order ID: ${response.data.orderId}`);
      console.log('Order Response:', response.data);

      // Reset form
      setFormData({
        title: '',
        author: '',
        pages: '',
        coverType: 'HARDCOVER',
        quantity: ''
      });

    } catch (err) {
      console.error('Error creating order:', err);

      // Better error messages
      if (err.code === 'ERR_NETWORK') {
        setError('Cannot connect to server. Make sure the API Gateway is running on http://localhost:8080');
      } else if (err.response) {
        // Server responded with error
        const status = err.response.status;
        const data = err.response.data;

        if (status === 400) {
          // Validation error
          const validationErrors = Object.entries(data)
            .map(([field, msg]) => `${field}: ${msg}`)
            .join(', ');
          setError(`Validation Error: ${validationErrors}`);
        } else if (status === 404) {
          setError('Endpoint not found. Check if the API Gateway URL is correct.');
        } else if (status === 500) {
          setError('Server error. Check backend logs.');
        } else {
          setError(`Error ${status}: ${data.message || 'Unknown error'}`);
        }
      } else {
        setError('Failed to create order. Please try again.');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="create-order-container">
      <div className="create-order-card">
        <button className="back-button" onClick={() => navigate('/')}>
          ← Back
        </button>

        <h1>Create Book Order</h1>
        <p className="description">Fill in the details to create a new book production order</p>

        {error && <div className="alert alert-error">{error}</div>}
        {success && <div className="alert alert-success">{success}</div>}

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="title">Book Title *</label>
            <input
              type="text"
              id="title"
              name="title"
              value={formData.title}
              onChange={handleChange}
              required
              placeholder="Enter book title"
            />
          </div>

          <div className="form-group">
            <label htmlFor="author">Author *</label>
            <input
              type="text"
              id="author"
              name="author"
              value={formData.author}
              onChange={handleChange}
              required
              placeholder="Enter author name"
            />
          </div>

          <div className="form-row">
            <div className="form-group">
              <label htmlFor="pages">Number of Pages *</label>
              <input
                type="number"
                id="pages"
                name="pages"
                value={formData.pages}
                onChange={handleChange}
                required
                min="1"
                placeholder="320"
              />
            </div>

            <div className="form-group">
              <label htmlFor="coverType">Cover Type *</label>
              <select
                id="coverType"
                name="coverType"
                value={formData.coverType}
                onChange={handleChange}
                required
              >
                <option value="HARDCOVER">Hardcover</option>
                <option value="SOFTCOVER">Softcover</option>
                <option value="SPIRAL">Spiral Bound</option>
              </select>
            </div>
          </div>

          <div className="form-group">
            <label htmlFor="quantity">Quantity *</label>
            <input
              type="number"
              id="quantity"
              name="quantity"
              value={formData.quantity}
              onChange={handleChange}
              required
              min="1"
              placeholder="100"
            />
          </div>

          <button type="submit" className="submit-button" disabled={loading}>
            {loading ? 'Creating Order...' : 'Create Order'}
          </button>
        </form>
      </div>
    </div>
  );
}

export default CreateOrderPage;