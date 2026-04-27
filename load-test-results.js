import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    thresholds: {
        http_req_duration: ['p(95)<1000'], // Ліміт часу на агрегацію — 1 секунда
        http_req_failed: ['rate<0.01'],    // Мінімум помилок
    },
    stages: [
        { duration: '10s', target: 20 },
        { duration: '30s', target: 20 },
        { duration: '10s', target: 0 },
    ],
};

export function setup() {
    console.log("🛠 Створюємо чисте опитування для тесту результатів...");

    // Створюємо опитування з питанням типу 3 (Оцінка), щоб перевірити математику сервера
    const newSurveyPayload = JSON.stringify({
        title: "Clean Results Load Test Survey",
        isActive: true,
        expiresAt: "2026-12-31T23:59:59.000Z",
        questions: [
            { text: "Rate the platform from 1 to 10", type: 3, isRequired: true, order: 1 }
        ]
    });

    const params = { headers: { 'Content-Type': 'application/json' } };
    const postRes = http.post('http://localhost:5236/api/surveys', newSurveyPayload, params);

    const createdSurvey = JSON.parse(postRes.body);
    const surveyId = createdSurvey.id || createdSurvey.Id;
    const qList = createdSurvey.questions || createdSurvey.Questions;
    const questionId = qList[0].id || qList[0].Id;

    console.log("📝 Додаємо правильну числову відповідь...");

    // Відправляємо ЧИСЛО "5", щоб сервер зміг нормально порахувати Average
    const answerPayload = JSON.stringify({
        RespondentEmail: "test_results_user@loadtest.com",
        Answers: [{ QuestionId: questionId, Value: "5" }]
    });
    http.post(`http://localhost:5236/api/surveys/${surveyId}/respond`, answerPayload, params);

    console.log(`✅ Готово! Тестуємо результати опитування: ${surveyId}`);
    return { surveyId: surveyId };
}

export default function (data) {
    const res = http.get(`http://localhost:5236/api/surveys/${data.surveyId}/results`);

    check(res, {
        'status is 200': (r) => r.status === 200,
        'has results': (r) => r.status === 200 && r.body.includes('totalResponses')
    });

    sleep(1);
}