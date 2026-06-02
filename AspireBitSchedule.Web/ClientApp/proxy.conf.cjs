const target = process.env.BACKEND_BASE_URL || 'https://localhost:7273';

module.exports = {
  '/api': {
    target,
    secure: false,
    changeOrigin: true,
    logLevel: 'info'
  }
};
