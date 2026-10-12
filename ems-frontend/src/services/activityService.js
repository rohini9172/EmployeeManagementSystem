import api from './api';

export const getRecentlyViewed = () => api.get('/activity/recent');
export const getPopularEmployees = () => api.get('/activity/popular');
export const getActivitySummary = () => api.get('/activity/summary');

// Fetches Redis Hash data for a specific employee (viewCount + lastViewed)
export const getEmployeeStats = (id) => api.get(`/activity/employee/${id}/stats`);
