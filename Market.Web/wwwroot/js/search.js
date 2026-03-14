document.addEventListener('DOMContentLoaded', () => {
    const resultsContainer = document.querySelector('#auction-results');
    const form = document.querySelector('#search-form');

    if (!resultsContainer || !form) {
        return;
    }

    let debounceTimer;

    function setLoading(isLoading) {
        if (isLoading) {
            resultsContainer.classList.add('results-loading');
        } else {
            resultsContainer.classList.remove('results-loading');
        }
    }

    async function searchWithFilters() {
        const params = new URLSearchParams(new FormData(form));
        
        setLoading(true);

        try {
            const response = await fetch(`/Auctions/Search?${params.toString()}`, {
                method: 'GET'
            });

            if (!response.ok) {
                return;
            }

            const html = await response.text();
            resultsContainer.innerHTML = html;
        } finally {
            setLoading(false);
        }
    }

    ['input'].forEach(eventType => {
        form.addEventListener(eventType, (e) => {
            if (e.target.tagName === 'BUTTON') return;
            
            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(searchWithFilters, 300);
        });
    });
});
    
