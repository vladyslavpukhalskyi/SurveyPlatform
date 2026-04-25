import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    thresholds: {
        http_req_duration: ['p(95)<500'], // 95% запитів мають бути швидшими за 500мс
    },
    stages: [
        { duration: '20s', target: 50 }, // Швидко наганяємо до 50 користувачів
        { duration: '40s', target: 50 }, // Тримаємо навантаження
        { duration: '10s', target: 0 },
    ],
};

export default function () {
    // Ми тестуємо GET запит, бо він навантажує базу при великій кількості даних
    const res = http.get('http://localhost:5236/api/surveys');
    check(res, { 'is status 200': (r) => r.status === 200 });
    sleep(0.5);
}