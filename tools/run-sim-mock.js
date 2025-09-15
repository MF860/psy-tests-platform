/// tools/run-sim-mock.js
// Mock simulation script for PsyApi
const NATIONAL_ID = '5555555555';

// Mock API responses
const mockResponses = {
    startSession: {
        sessionId: 12345,
        totalQuestions: 80
    },
    firstQuestion: {
        id: 101,
        text_ar: "هل تشعر بالقلق غالبا؟",
        type: "LIKERT"
    },
    secondQuestion: {
        id: 102,
        text_ar: "هل تجد صعوبة في النوم؟",
        type: "LIKERT"
    },
    submitTest: {
        message: "submitted",
        sessionId: 12345
    }
};

// Track which question to return next
let questionIndex = 0;

// Mock API functions
const mockApi = {
    post: async (endpoint, data) => {
        console.log(`POST ${endpoint}`, data);

        if (endpoint === '/sessions/start') {
            return { data: mockResponses.startSession };
        } else if (endpoint.includes('/answer')) {
            questionIndex++; // Move to next question after answering
            return { data: { message: "Answer submitted successfully" } };
        } else if (endpoint.includes('/submit')) {
            return { data: mockResponses.submitTest };
        }

        return { data: {} };
    },
    get: async (endpoint) => {
        console.log(`GET ${endpoint}`);

        if (endpoint.includes('/next')) {
            if (questionIndex === 0) {
                return { data: mockResponses.firstQuestion };
            } else if (questionIndex === 1) {
                return { data: mockResponses.secondQuestion };
            } else {
                return { data: { message: "completed" } };
            }
        }

        return { data: {} };
    }
};

// Helper function to pick values from an object
function pick(obj, keys) {
    for (const k of keys) if (obj && obj[k] !== undefined) return obj[k];
    return undefined;
}

// Main simulation function
async function run() {
    try {
        console.log('=== PsyApi E2E Simulation (Mock) ===');
        console.log('Connecting to Mock API');

        // 1) Start session
        console.log('Starting session for national ID:', NATIONAL_ID);
        const startRes = await mockApi.post('/sessions/start', { national_id: NATIONAL_ID });
        console.log('Start session response:', startRes.data);

        const sessionId = pick(startRes.data, ['sessionId', 'id', 'session_id']);
        if (!sessionId) {
            throw new Error('Could not extract session ID from response');
        }
        console.log('Session started:', sessionId);

        // Helper to get next question
        async function getNext() {
            console.log('Getting next question for session:', sessionId);
            const r = await mockApi.get(`/sessions/${sessionId}/next`);
            console.log('Get next question response:', r.data);

            if (r.data && r.data.message === 'completed') return { completed: true };

            const itemId = pick(r.data, ['itemId', 'id', 'item_id']);
            const text = pick(r.data, ['text_ar', 'textAr', 'text']);

            if (!itemId || !text) {
                throw new Error('Could not extract item ID or text from response');
            }

            return {
                completed: false,
                itemId: itemId,
                text: text,
                type: r.data.type,
            };
        }

        // Q1
        console.log('Getting first question...');
        const q1 = await getNext();
        console.log('Q1:', q1.itemId, q1.text);

        console.log('Submitting answer for Q1...');
        await mockApi.post(`/sessions/${sessionId}/answer`, { item_id: q1.itemId, answer: 'موافق' });

        // Q2
        console.log('Getting second question...');
        const q2 = await getNext();
        console.log('Q2:', q2.itemId, q2.text);

        console.log('Submitting answer for Q2...');
        await mockApi.post(`/sessions/${sessionId}/answer`, { item_id: q2.itemId, answer: 'غير موافق' });

        // Submit test
        console.log('Submitting test...');
        const submitRes = await mockApi.post(`/sessions/${sessionId}/submit`);
        console.log('Submit test response:', submitRes.data);

        const submitStatus = pick(submitRes.data, ['message', 'status']);

        console.log('\n=== Final Summary ===');
        console.log(`User: ${NATIONAL_ID}`);
        console.log(`- SessionId: ${sessionId}`);
        console.log(`- First Question: ${q1.itemId} ${q1.text}`);
        console.log(`- Answer1: "موافق"`);
        console.log(`- Second Question: ${q2.itemId} ${q2.text}`);
        console.log(`- Answer2: "غير موافق"`);
        console.log(`- Submit Status: ${submitStatus}`);

    } catch (err) {
        console.error('Simulation failed:');
        console.error('Error message:', err.message);
        console.error('Stack:', err.stack);
        process.exit(1);
    }
}

// Run the simulation
run();
