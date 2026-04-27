import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    // Налаштування етапів навантаження
    stages: [
        { duration: '15s', target: 10 }, // Плавно піднімаємо до 10 користувачів за 15 секунд
        { duration: '30s', target: 10 }, // Тримаємо навантаження (10 користувачів) 30 секунд
        { duration: '15s', target: 0 },  // Плавно опускаємо до 0
    ],
};

export function setup() {
    console.log("🛠 Створюємо опитування для стрес-тесту...");

    const newSurveyPayload = JSON.stringify({
        title: "K6 Stress Test Survey",
        isActive: true,
        expiresAt: "2026-12-31T23:59:59.000Z",
        questions: [
            { text: "Text question?", type: 0, isRequired: true, order: 1 }
        ]
    });

    const params = { headers: { 'Content-Type': 'application/json' } };
    const postRes = http.post('http://localhost:5236/api/surveys', newSurveyPayload, params);

    const createdSurvey = JSON.parse(postRes.body);
    return {
        surveyId: createdSurvey.id || createdSurvey.Id,
        questionId: (createdSurvey.questions || createdSurvey.Questions)[0].id || (createdSurvey.questions || createdSurvey.Questions)[0].Id
    };
}

export default function (data) {
    // Використовуємо Math.random() + Date.now() для унікальних email при великому навантаженні
    const payload = JSON.stringify({
        RespondentEmail: `stress_${Date.now()}_${Math.floor(Math.random() * 1000)}@loadtest.com`,
        Answers: [
            {
                QuestionId: data.questionId,
                Value: "Stress Test Answer"
            }
        ]
    });

    const params = { headers: { 'Content-Type': 'application/json' } };
    const res = http.post(`http://localhost:5236/api/surveys/${data.surveyId}/respond`, payload, params);

    // Автоматична перевірка успішності запиту
    check(res, {
        'status is 200': (r) => r.status === 200,
    });

    sleep(1); // Коротка пауза між ітераціями
}