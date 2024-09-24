[...document.querySelectorAll(".article section.h2-related")]
    .map(s => ({
        Name: s.querySelector("h3").innerText,
        PropertyName: [...s.querySelectorAll("section > p > code")].map(c => c.innerText)[0],
        Answers: [...s.querySelectorAll("table")].map(t => {
            if (t.innerText.includes("Before formatting")) {
                return {
                    Name: t.querySelector("th:nth-child(2)").innerText.substring(18),
                    PropertyValue: t.querySelector("th:nth-child(2)").innerText.substring(18),
                    Example: {
                        Code: t.querySelector("td:nth-child(2) code").innerText,
                    },
                };
            } else {
                return {
                    Name: t.querySelector("th").innerText,
                    PropertyValue: t.querySelector("th").innerText,
                    Example: {
                        Code: t.querySelector("td code").innerText,
                    },
                };
            }
        }),
    }));
