function load() {
    console.log("Loading data");

    // In the future, we probably want to use a more robust way to determine the URL of the server, but for now we'll just hardcode it.
    fetch('https://localhost:7146/DirectoryBrowsing/directories')
        .then(response => response.json())
        .then(json => {
            console.log('json', json);
            const insertionpoint = document.getElementById('directorytree');
            // Clear any existing contents
            insertionpoint.innerHTML = '';
            // Render the root entity (the JSON provided)
            renderEntity(json, insertionpoint);
            attachCaretHandlers();
        })
        .catch(err => console.error('Failed to load directory tree', err));
}

// Recursively render an entity into a parent <ul>
function renderEntity(entity, parentUl) {
    if (!entity) return;

    // Derive display name from path
    const name = (entity.path || '').split('\\').pop() || entity.entityType;

    // For folders: create a caret span and nested <ul>
    if (entity.entityType === 'Folder') {
        const li = document.createElement('li');

        const span = document.createElement('span');
        span.className = 'caret';
        span.textContent = name;
        li.appendChild(span);

        const nested = document.createElement('ul');
        nested.className = 'nested';
        // Render children
        if (Array.isArray(entity.subentities)) {
            entity.subentities.forEach(child => renderEntity(child, nested));
        }
        li.appendChild(nested);
        parentUl.appendChild(li);
    } else { // File or unknown -> render as plain list item
        const li = document.createElement('li');
        const fileSpan = document.createElement('span');
        fileSpan.className = 'file';
        fileSpan.textContent = name;
        // store full path for later use if needed
        fileSpan.dataset.path = entity.path || '';
        // We want users to be able to click on files to download them
        fileSpan.onclick = function () {
            const path = this.dataset.path;
            console.log('Clicked file with path:', path);

        }
        li.appendChild(fileSpan);
        parentUl.appendChild(li);
    }
}

function downloadFile(path) {

}

// Attach click handlers to caret elements to toggle nested lists
function attachCaretHandlers() {
    const togglers = document.getElementsByClassName('caret');
    for (let i = 0; i < togglers.length; i++) {
        const t = togglers[i];
        // avoid adding duplicate handlers
        if (t._hasClick) continue;
        t._hasClick = true;
        t.addEventListener('click', function () {
            const nested = this.nextElementSibling;
            if (nested) nested.classList.toggle('active');
            this.classList.toggle('caret-down');
        });
    }
}