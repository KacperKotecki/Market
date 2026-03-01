export async function generateDescription(formData) {
    const response = await fetch('/Auctions/GenerateDescription', {
        method: 'POST',
        body: formData
    });

    if (!response.ok) {
        const err = await response.json().catch(() => ({}));
        throw new Error(err.error || "Wystąpił błąd serwera");
    }

    const data = await response.json();

    if (data.title && data.title.startsWith("ERROR:")) {
        throw new Error(data.title.replace("ERROR:", "").trim());
    }

    return data;
}