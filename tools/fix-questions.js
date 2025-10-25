const axios = require('axios');

const BASE_URL = process.env.BASE_URL || 'http://localhost:5019/api';
const ADMIN_USER = process.env.ADMIN_USER || 'root';
const ADMIN_PASS = process.env.ADMIN_PASS || 'StrongAdmin!23!';

const api = axios.create({ baseURL: BASE_URL, timeout: 20000, headers: { 'Content-Type': 'application/json' } });

async function adminLogin() {
  const r = await api.post('/admin/login', { username: ADMIN_USER, password: ADMIN_PASS });
  const token = r.data?.token;
  if (!token) throw new Error('Admin login failed: no token');
  api.defaults.headers.Authorization = `Bearer ${token}`;
  return token;
}

async function main() {
  console.log('=== Fixing Questions ===');
  console.log('Logging in as admin...');
  await adminLogin();

  console.log('Fixing questions...');
  const r = await api.post('/admin/questions/fix');
  console.log('Fix result:', JSON.stringify(r.data, null, 2));

  console.log('\nValidating questions after fix...');
  const v = await api.get('/admin/questions/validate');
  console.log('Validation result:', JSON.stringify(v.data, null, 2));
}

main().catch((e) => {
  console.error('Error:', e?.response?.data || e.message);
  process.exit(1);
});
