// Fuzzy-searchable lists built on List.js.
// Markup contract:
//   <div data-searchable-list="title,author,isbn">
//       <input class="fuzzy-search" />
//       <ul class="list"><li><span class="title"></span>...</li></ul>
//   </div>

// Characters of the term must appear in order in the text (subsequence match),
// so "hpotter" matches "Harry Potter". Every whitespace-separated term must match.
function fuzzyMatch(term, text) {
    let i = 0;
    for (let j = 0; j < text.length && i < term.length; j++) {
        if (text[j] === term[i]) i++;
    }
    return i === term.length;
}

function makeFuzzySearch(list) {
    return (searchString, columns) => {
        const terms = searchString.split(/\s+/).filter(Boolean);
        for (const item of list.items) {
            const haystack = columns.map(c => String(item.values()[c] ?? '')).join(' ').toLowerCase();
            item.found = terms.every(t => fuzzyMatch(t, haystack));
        }
    };
}

window.initSearchableLists = () => {
    document.querySelectorAll('[data-searchable-list]').forEach(el => {
        const valueNames = el.dataset.searchableList.split(',');
        // searchClass points at a class nothing uses: List.js's built-in input
        // handler can't take a custom search function, so we bind the input below.
        const list = new List(el, { valueNames, listClass: 'list', searchClass: 'list-js-unused' });
        const fuzzy = makeFuzzySearch(list);
        const input = el.querySelector('.fuzzy-search');
        if (input) {
            // oninput (not addEventListener) so re-running after enhanced navigation
            // replaces the old handler instead of stacking a second one.
            input.oninput = () => list.search(input.value, valueNames, fuzzy);
        }
    });
};

document.addEventListener('DOMContentLoaded', window.initSearchableLists);
// Re-run after Blazor enhanced navigation swaps the page body
// (e.g. returning to /books after saving a form).
if (window.Blazor) {
    Blazor.addEventListener('enhancedload', window.initSearchableLists);
}
