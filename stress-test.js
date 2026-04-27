import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    thresholds: {
        http_req_duration: ['p(95)<500'],
        http_req_failed: ['rate<0.01'],
    },
    stages: [
        { duration: '20s', target: 50 },
        { duration: '40s', target: 50 },
        { duration: '10s', target: 0 },
    ],
};

export function setup() {
    // 1. Спочатку пробуємо знайти готове опитування
    const getRes = http.get('http://localhost:5236/api/surveys');
    if (getRes.status === 200) {
        const surveys = JSON.parse(getRes.body);
        const targetSurvey = surveys.find(s =>
            (s.questions && s.questions.length > 0) ||
            (s.Questions && s.Questions.length > 0)
        );

        if (targetSurvey) {
            console.log("✅ Знайдено існуюче опитування. Починаємо тест...");
            return {
                surveyId: targetSurvey.id || targetSurvey.Id,
                questionId: (targetSurvey.questions || targetSurvey.Questions)[0].id || (targetSurvey.questions || targetSurvey.Questions)[0].Id
            };
        }
    }

    // 2. Якщо опитувань немає — k6 СТВОРЮЄ ЙОГО САМ!
    console.log("⚠️ Не знайдено опитувань з питаннями. k6 створює нове опитування автоматично...");

    const newSurveyPayload = JSON.stringify({
        id: "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        title: "K6 Auto Generated Survey",
        description: "Created automatically by load test",
        isActive: true,
        createdAt: new Date().toISOString(),
        expiresAt: "2026-12-31T23:59:59.000Z",
        questions: [
            {
                id: "3fa85f64-5717-4562-b3fc-2c963f66afa7",
                text: "Are you ready for the load test?",
                type: 0,
                isRequired: true,
                order: 1
            }
        ]
    });

    const params = { headers: { 'Content-Type': 'application/json' } };
    const postRes = http.post('http://localhost:5236/api/surveys', newSurveyPayload, params);

    if (postRes.status !== 201 && postRes.status !== 200) {
        console.error("❌ k6 не зміг створити опитування! Статус:", postRes.status);
        console.error("Відповідь сервера:", postRes.body);
        throw new Error("Критична помилка: API відхилило створення опитування.");
    }

    console.log("✅ Опитування успішно створено!");
    const createdSurvey = JSON.parse(postRes.body);

    return {
        surveyId: createdSurvey.id || createdSurvey.Id,
        questionId: (createdSurvey.questions || createdSurvey.Questions)[0].id || (createdSurvey.questions || createdSurvey.Questions)[0].Id
    };
}

export default function (data) {
    const payload = JSON.stringify({
        RespondentEmail: `user_${__VU}_${__ITER}@loadtest.com`,
        Answers: [
            {
                QuestionId: data.questionId,
                Value: "Stress Test Answer"
            }
        ]
    });

    const params = { headers: { 'Content-Type': 'application/json' } };
    const res = http.post(`http://localhost:5236/api/surveys/${data.surveyId}/respond`, payload, params);

    check(res, { 'status is 200': (r) => r.status === 200 });
    sleep(1);
}