export async function ensureDevToken(hours = 24, role = 'Administrator', sub = 'dev-user') {
  if (!import.meta.env.DEV) return null;

  try {
    const existing = localStorage.getItem('auth_token');
    if (existing) return existing;

    const url = `/dev/token?hours=${hours}&role=${encodeURIComponent(role)}&sub=${encodeURIComponent(sub)}`;
    const resp = await fetch(url, { method: 'GET' });
    if (!resp.ok) {
      console.warn('Failed to fetch dev token:', resp.status, resp.statusText);
      return null;
    }
    const body = await resp.json();
    if (body?.token) {
      localStorage.setItem('auth_token', body.token);
      console.info('Dev token stored in localStorage');
      return body.token;
    }
    return null;
  } catch (err) {
    console.warn('Error obtaining dev token', err);
    return null;
  }
}
