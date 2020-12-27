module.exports = {
    purge: {
        // purging only happens if environment === "production", see csproj file
        content: ["./Pages/**/*.razor", "./Shared/**/*.razor"],
    },
    darkMode: 'media', // or 'media' or 'class'
    theme: {
        extend: {
        },
    },
    variants: {
        extend: {},
    },
    plugins: [require('@tailwindcss/forms')],
};
